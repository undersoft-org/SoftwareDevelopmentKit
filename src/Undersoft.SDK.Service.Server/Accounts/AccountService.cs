using System.Linq.Expressions;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Undersoft.SDK.Service.Access;
using Undersoft.SDK.Service.Operation;
using Undersoft.SDK.Service.Server.Accounts.Email;
using Undersoft.SDK.Utilities;

namespace Undersoft.SDK.Service.Server.Accounts;

public class AccountService<TAccount> : IAccessService<TAccount>
    where TAccount : class, IOrigin, IAuthorization
{
    private IServicer _servicer;
    private IAccountManager _manager;
    private IEmailSender _email;
    private string _signUpRole = "User";
    private int _openingCount = -1;

    private static ISeries<string> TokenRegistry = new Registry<string>();

    public AccountService() { }

    public AccountService(IServicer servicer, IAccountManager accountManager, IEmailSender email)
    {
        _servicer = servicer;
        _manager = accountManager;
        _email = email;
        HandlePrimeAccount();
    }

    public async Task<IAuthorization> SignIn(IAuthorization auth)
    {
        var account = await ConfirmEmail(await Authenticate(auth));

        if (account.Credentials.Authenticated && account.Credentials.EmailConfirmed)
        {
            account.Credentials.SessionToken = await _manager.GetToken(account);
            account.Notes = new OperationNotes()
            {
                Success = "Signed in",
                Status = AccessStatus.SignedIn
            };
            var _account = await _manager.GetByEmail(account.Credentials.Email);
            var claims = await _manager.User.GetClaimsAsync(_account.User);
            await _manager.SignIn.SignInWithClaimsAsync(
                _account.User,
                account.Credentials.SaveAccountInCookies,
                claims
            );
            var a = typeof(TAccount).New<TAccount>();
            account.Credentials.PatchTo(a.Credentials);
            dynamic registered = await Registered(a);
            if (registered != null && registered.Personal != null)
                ((object)registered.Personal).PatchTo(account.Credentials);
        }
        return account;
    }

    public async Task<IAuthorization> SignUp(IAuthorization auth)
    {
        var _creds = auth.Credentials;
        if (!_manager.TryGetByEmail(_creds.Email, out var account))
        {
            account = await _manager.SetUser(
                _creds.UserName,
                _creds.Email,
                _creds.Password,
                new string[] { _signUpRole }
            );
            if (!account.Notes.IsSuccess)
            {
                auth.Notes = account.Notes;
                return auth;
            }
        }
        else
        {
            auth.Notes = new OperationNotes()
            {
                Errors = "Account already exists!!",
                Status = AccessStatus.Failure
            };
            return auth;
        }

        return await ConfirmEmail(await Authenticate(auth));
    }

    public async Task<IAuthorization> SignOut(IAuthorization auth)
    {
        var account = await SignedUp(auth);

        if (account.Credentials.IsLockedOut)
        {
            var principal = await _manager.SignIn.CreateUserPrincipalAsync(
                await _manager.User.FindByEmailAsync(account.Credentials.Email)
            );
            if (_manager.SignIn.IsSignedIn(principal))
                await _manager.SignIn.SignOutAsync();
            account.Notes = new OperationNotes()
            {
                Success = "Signed out",
                Status = AccessStatus.SignedOut
            };
        }
        return account;
    }

    public async Task<IAuthorization> SignedIn(IAuthorization auth)
    {
        var account = await SignedUp(auth);

        if (!account.Credentials.IsLockedOut)
        {
            var token = await _manager.RenewToken(auth.Credentials.SessionToken);
            if (token != null)
            {
                account.Credentials.SessionToken = token;
                account.Notes = new OperationNotes()
                {
                    Success = "Token renewed",
                    Status = AccessStatus.Succeed
                };
            }
            else
            {
                account.Credentials.SessionToken = null;
                account.Notes = new OperationNotes()
                {
                    Errors = "Invalid token ",
                    Status = AccessStatus.Failure
                };
            }
        }
        return account;
    }

    public async Task<IAuthorization> SignedUp(IAuthorization auth)
    {
        var _creds = auth.Credentials;
        if (!_manager.TryGetByEmail(_creds.Email, out var account))
        {
            _creds.Password = null;
            _creds.IsLockedOut = false;
            _creds.Authenticated = false;
            _creds.EmailConfirmed = false;
            _creds.PhoneNumberConfirmed = false;
            _creds.RegistrationCompleted = false;
            account = new Account() { Credentials = _creds };
            account.Notes = new OperationNotes()
            {
                Errors = "Invalid email",
                Status = AccessStatus.InvalidEmail
            };
            return account;
        }
        var creds = account.Credentials;
        creds.PatchFrom(_creds);
        if (account.User.LockoutEnabled)
            creds.IsLockedOut = account.User.IsLockedOut;
        else
            creds.IsLockedOut = false;
        creds.Authenticated = false;
        creds.EmailConfirmed = account.User.EmailConfirmed;
        creds.PhoneNumberConfirmed = account.User.PhoneNumberConfirmed;
        creds.RegistrationCompleted = account.User.RegistrationCompleted;
        return await Task.FromResult(account);
    }

    public async Task<IAuthorization> Authenticate(IAuthorization auth)
    {
        auth = await SignedUp(auth);

        var credentials = auth?.Credentials;

        if (auth.Notes.Status == AccessStatus.InvalidEmail)
        {
            credentials.Password = null;
            return auth;
        }

        if (!credentials.IsLockedOut)
        {
            if (await _manager.CheckPassword(credentials.Email, credentials.Password) == null)
            {
                credentials.Authenticated = false;
                auth.Notes = new OperationNotes()
                {
                    Errors = "Invalid password",
                    Status = AccessStatus.InvalidPassword
                };
            }
            else
            {
                credentials.Authenticated = true;
                auth.Notes = new OperationNotes() { Info = "Pasword is valid", };
            }
        }
        else
        {
            credentials.Authenticated = false;
            auth.Notes = new OperationNotes()
            {
                Errors = "Account is locked out",
                Status = AccessStatus.InvalidPassword
            };
        }
        credentials.Password = null;
        return auth;
    }

    public async Task<IAuthorization> ConfirmEmail(IAuthorization auth)
    {
        if (
            auth != null
            && !auth.Credentials.IsLockedOut
            && auth.Credentials.Authenticated
        )
        {
            var credentials = auth.Credentials;
            if (!credentials.EmailConfirmed)
            {
                if (credentials.EmailConfirmationToken != null)
                {
                    var _code = int.Parse(credentials.EmailConfirmationToken);
                    var _token = TokenRegistry.Get(_code);
                    var result = await _manager.User.ConfirmEmailAsync(
                        (await _manager.GetByEmail(credentials.Email)).User,
                        _token
                    );
                    TokenRegistry.Remove(_code);
                    if (result.Succeeded)
                    {
                        HandlePrimeAccount();
                        credentials.EmailConfirmed = true;
                        auth.Credentials.Authenticated = true;
                        auth.Notes = new OperationNotes()
                        {
                            Success = "Email has been confirmed",
                            Status = AccessStatus.EmailConfirmed
                        };
                        this.Success<Accesslog>(auth.Notes.Success, auth);
                    }
                    else
                    {
                        auth.Notes = new OperationNotes()
                        {
                            Errors = result
                                .Errors.Select(d => d.Description)
                                .Aggregate((a, b) => a + ". " + b),
                            Status = AccessStatus.Failure
                        };
                        this.Failure<Accesslog>(auth.Notes.Errors, auth);
                    }
                    credentials.EmailConfirmationToken = null;
                    return auth;
                }

                var token = await _manager.User.GenerateEmailConfirmationTokenAsync(
                    (await _manager.GetByEmail(credentials.Email)).User
                );
                var code = Math.Abs(token.UniqueKey32());
                TokenRegistry.Add(code, token);
                var sender = _servicer.GetService<IEmailSender>();
                await sender.SendEmailAsync(
                    credentials.Email,
                    "Verfication code to confirm your email address and proceed with auth registration process",
                    EmailTemplate.GetVerificationCodeMessage(code.ToString())
                );

                auth.Notes = new OperationNotes()
                {
                    Info = "Please check your email",
                    Status = AccessStatus.EmailNotConfirmed,
                };
                this.Security<Accesslog>(auth.Notes.Info, auth);
            }
            else
            {
                auth.Notes = new OperationNotes() { Info = "Email was confirmed" };
                auth.Credentials.Authenticated = true;
            }
        }
        return auth;
    }

    public async Task<IAuthorization> ResetPassword(IAuthorization auth)
    {
        auth = await SignedUp(auth);

        if (auth != null && !auth.Credentials.IsLockedOut)
        {
            var _creds = auth.Credentials;
            if (_creds.PasswordResetToken != null)
            {
                IdentityResult result = null;
                var newpassword = GenerateRandomPassword();
                var _code = int.Parse(_creds.PasswordResetToken);
                var _token = TokenRegistry.Get(_code);
                if (_token != null)
                {
                    result = await _manager.User.ResetPasswordAsync(
                        (await _manager.GetByEmail(_creds.Email)).User,
                        _token,
                        newpassword
                    );
                }
                TokenRegistry.Remove(_code);
                if (result != null && result.Succeeded)
                {
                    auth.Credentials.Authenticated = true;
                    auth.Notes = new OperationNotes()
                    {
                        Success = "Password has been reset",
                        Status = AccessStatus.ResetPasswordConfirmed
                    };
                    this.Success<Accesslog>(auth.Notes.Success, auth);
                    _ = _servicer.Serve<IEmailSender>(e =>
                        e.SendEmailAsync(
                            _creds.Email,
                            "Password reset succeed. Now You can sign in, using generated password inside this message. Then change it from the profile settings",
                            EmailTemplate.GetResetPasswordMessage(newpassword)
                        )
                    );
                }
                else
                {
                    auth.Credentials.Authenticated = false;
                    auth.Notes = new OperationNotes()
                    {
                        Errors = result
                            .Errors.Select(d => d.Description)
                            .Aggregate((a, b) => a + ". " + b),
                        Status = AccessStatus.Failure
                    };
                    this.Failure<Accesslog>(auth.Notes.Errors, auth);
                }
                _creds.PasswordResetToken = null;
                return auth;
            }

            var token = await _manager.User.GeneratePasswordResetTokenAsync(
                (await _manager.GetByEmail(_creds.Email)).User
            );
            var code = Math.Abs(token.UniqueKey32());
            TokenRegistry.Add(code, token);
            _ = _servicer.Serve<IEmailSender>(e =>
                e.SendEmailAsync(
                    _creds.Email,
                    "Verfication code to confirm your decision about resetting the password and sending generated one to your email",
                    EmailTemplate.GetVerificationCodeMessage(code.ToString())
                )
            );

            auth.Notes = new OperationNotes()
            {
                Info = "Please check your email to confirm password reset",
                Status = AccessStatus.ResetPasswordNotConfirmed,
            };
            auth.Credentials.Authenticated = false;
            this.Security<Accesslog>(auth.Notes.Info, auth);
        }
        return auth;
    }

    public async Task<IAuthorization> ChangePassword(IAuthorization auth)
    {
        auth = await Authenticate(auth);

        if (auth != null && auth.Credentials.Authenticated)
        {
            var credentials = auth.Credentials;
            var result = await _manager.User.ChangePasswordAsync(
                (await _manager.GetByEmail(credentials.Email)).User,
                credentials.Password,
                credentials.NewPassword
            );

            if (result.Succeeded)
            {
                auth.Notes = new OperationNotes()
                {
                    Success = "Password has been changed",
                    Status = AccessStatus.Succeed
                };
                this.Success<Accesslog>(auth.Notes.Success, auth);
            }
            else
            {
                auth.Credentials.Authenticated = false;
                auth.Notes = new OperationNotes()
                {
                    Errors = result
                        .Errors.Select(d => d.Description)
                        .Aggregate((a, b) => a + ". " + b),
                    Status = AccessStatus.Failure
                };
                this.Failure<Accesslog>(auth.Notes.Errors, auth);
            }
            credentials.Password = null;
            return auth;
        }
        return auth;
    }

    //public async Task<IAuthorization> CompleteRegistration(IAuthorization account)
    //{
    //    var _creds = account.Credentials;
    //    if (!_creds.RegistrationCompleted)
    //    {
    //        var _account = await _manager.GetByEmail(_creds.Email);
    //        if (_account == null)
    //        {
    //            account.Notes = new OperationNotes()
    //            {
    //                Errors = "Account not found",
    //                Status = AccessStatus.RegistrationNotCompleted
    //            };
    //            this.Failure<Accesslog>(account.Notes.Success, account);
    //            return account;
    //        }

    //        if (_creds.RegistrationCompleteToken != null)
    //        {
    //            var _code = int.Parse(_creds.RegistrationCompleteToken);
    //            var _token = TokenRegistry.Get(_code);
    //            TokenRegistry.Remove(_code);
    //            var isValid = await _manager.User.VerifyUserTokenAsync(
    //                _account.User,
    //                "AccountRegistrationProcessTokenProvider",
    //                "Registration",
    //                _token
    //            );

    //            if (isValid)
    //            {
    //                _account.User.RegistrationCompleted = true;

    //                if ((await _manager.User.UpdateAsync(_account.User)).Succeeded)
    //                {
    //                    _creds.RegistrationCompleted = true;
    //                    _creds.Authenticated = true;
    //                    account.Notes = new OperationNotes()
    //                    {
    //                        Success = "Registration completed",
    //                        Status = AccessStatus.RegistrationCompleted
    //                    };
    //                    this.Success<Accesslog>(account.Notes.Success, account);
    //                }
    //                else
    //                {
    //                    this.Failure<Accesslog>(account.Notes.Errors, account);
    //                }
    //            }
    //            else
    //            {
    //                account.Notes = new OperationNotes()
    //                {
    //                    Errors = "Registration not completed. Invalid verification code",
    //                    Status = AccessStatus.RegistrationNotCompleted
    //                };
    //                this.Failure<Accesslog>(account.Notes.Success, account);
    //            }

    //            _creds.RegistrationCompleteToken = null;
    //            return account;
    //        }

    //        var token = await _manager.User.GenerateUserTokenAsync(
    //            (await _manager.GetByEmail(_creds.Email)).User,
    //            "AccountRegistrationProcessTokenProvider",
    //            "Registration"
    //        );
    //        var code = Math.Abs(token.UniqueKey32());
    //        TokenRegistry.Add(code, token);
    //        _ = _servicer.Serve<IEmailSender>(e =>
    //            e.SendEmailAsync(
    //                _creds.Email,
    //                "Verfication code to confirm your email address and proceed with account registration process",
    //                EmailTemplate.GetVerificationCodeMessage(code.ToString())
    //            )
    //        );
    //        account.Notes = new OperationNotes()
    //        {
    //            Info = "Please confirm registration process",
    //            Status = AccessStatus.RegistrationNotConfirmed
    //        };
    //    }
    //    else
    //        account.Notes = new OperationNotes() { Info = "Registration was completed" };

    //    return account;
    //}

    public async Task<TAccount> Register(TAccount accessAccount)
    {
        var credentials = accessAccount.Credentials;
        var serverAccount = await _manager.GetByEmail(credentials.Email);

        if (serverAccount == null)
        {
            accessAccount.Notes = new OperationNotes()
            {
                Errors = "Account not found",
                Status = AccessStatus.RegistrationNotCompleted
            };
            this.Failure<Accesslog>(accessAccount.Notes.Success, accessAccount);
            return accessAccount;
        }

        accessAccount.PutTo(serverAccount);

        var accountUser = await _manager.User.FindByEmailAsync(credentials.Email);

        if (accessAccount.Notes.Status != AccessStatus.RegistrationNotCompleted)
        {
            accountUser.RegistrationCompleted = true;
            var claims = _manager.User.AddClaimsAsync(
                    accountUser, new[] {

                        new Claim("tenant_id", serverAccount.Tenant.Id.ToString()),
                        new Claim("organization_id", serverAccount.Organization.Id.ToString())
                    }
                );
            claims.Wait();

        }
        if ((await _manager.User.UpdateAsync(accountUser)).Succeeded)
        {
            if (accessAccount.Notes.Status != AccessStatus.RegistrationNotCompleted)
            {                
                credentials.RegistrationCompleted = true;
            }
            credentials.Authenticated = true;
            serverAccount.Notes = new OperationNotes()
            {
                Success = "Registration completed",
                Status = AccessStatus.RegistrationCompleted
            };
            this.Success<Accesslog>(serverAccount.Notes.Success, accessAccount);
        }
        else
        {
            this.Failure<Accesslog>(serverAccount.Notes.Errors, accessAccount);
        }

        serverAccount = await _manager.Accounts.Put(serverAccount, null);

        var count = await _manager.Accounts.Save(true);

        serverAccount.User = accountUser;
        serverAccount.PatchTo(accessAccount);
        accountUser.PatchTo(accessAccount.Credentials);
        serverAccount.Personal.PatchTo(accessAccount.Credentials);

        return accessAccount;
    }

    public async Task<TAccount> Unregister(TAccount accessAccount)
    {
        var credentials = accessAccount.Credentials;
        var serverAccount = await _manager.GetByEmail(credentials.Email);

        if (serverAccount == null)
        {
            accessAccount.Notes = new OperationNotes()
            {
                Errors = "Account not found",
                Status = AccessStatus.RegistrationNotCompleted
            };
            this.Failure<Accesslog>(accessAccount.Notes.Success, accessAccount);
            return accessAccount;
        }

        var accountUser = serverAccount.User;
        if (accountUser != null)
        {
            serverAccount = await _manager.Accounts.Delete(accountUser.Id);
            if (serverAccount != null)
            {
                serverAccount.User = accountUser;
                serverAccount.PatchTo(accessAccount);
                accountUser.PatchTo(accessAccount.Credentials);
                serverAccount.Personal.PatchTo(accessAccount.Credentials);
            }
        }
        return accessAccount;
    }

    public async Task<TAccount> Registered(TAccount accessAccount)
    {
        var credentials = accessAccount.Credentials;
        var serverAccount = await _manager.GetByEmail(credentials.Email);

        if (serverAccount == null)
        {
            accessAccount.Notes = new OperationNotes()
            {
                Errors = "Account not found",
                Status = AccessStatus.RegistrationNotCompleted
            };
            this.Failure<Accesslog>(accessAccount.Notes.Success, accessAccount);
            return accessAccount;
        }

        if (serverAccount.User != null)
        {
            if (serverAccount.Personal == null)
            {
                var account = _manager.Accounts.Query.Where(a => a.Id == serverAccount.User.Id).AsNoTracking().FirstOrDefault();
                if ((account != null))
                    account.PatchTo(serverAccount);
            }
            serverAccount.PutTo(accessAccount);
            serverAccount.User.PatchTo(accessAccount.Credentials);
            serverAccount.Personal.PatchTo(accessAccount.Credentials);        
        }
        return accessAccount;
    }

    public Task<ClaimsPrincipal?> RefreshAsync()
    {
        throw new Exception("Account service doesn't provide current state");
    }

    private int Count()
    {
        return _manager.Accounts.Query.Count();
    }

    private void HandlePrimeAccount()
    {
        if (_openingCount < 0)
            _openingCount = Count();
        if (_openingCount == 0)
            _signUpRole = "Administrator";
        else
            _signUpRole = "User";
    }

    public static string GenerateRandomPassword(PasswordOptions opts = null)
    {
        if (opts == null)
            opts = new PasswordOptions()
            {
                RequiredLength = 8,
                RequiredUniqueChars = 4,
                RequireDigit = true,
                RequireLowercase = true,
                RequireNonAlphanumeric = true,
                RequireUppercase = true
            };

        string[] randomChars = new[]
        {
            "ABCDEFGHJKLMNOPQRSTUVWXYZ", // uppercase
            "abcdefghijkmnopqrstuvwxyz", // lowercase
            "0123456789", // digits
            "!@$?_-" // non-alphanumeric
        };

        Random rand = new Random(Environment.TickCount);
        List<char> chars = new List<char>();

        if (opts.RequireUppercase)
            chars.Insert(
                rand.Next(0, chars.Count),
                randomChars[0][rand.Next(0, randomChars[0].Length)]
            );

        if (opts.RequireLowercase)
            chars.Insert(
                rand.Next(0, chars.Count),
                randomChars[1][rand.Next(0, randomChars[1].Length)]
            );

        if (opts.RequireDigit)
            chars.Insert(
                rand.Next(0, chars.Count),
                randomChars[2][rand.Next(0, randomChars[2].Length)]
            );

        if (opts.RequireNonAlphanumeric)
            chars.Insert(
                rand.Next(0, chars.Count),
                randomChars[3][rand.Next(0, randomChars[3].Length)]
            );

        for (
            int i = chars.Count;
            i < opts.RequiredLength || chars.Distinct().Count() < opts.RequiredUniqueChars;
            i++
        )
        {
            string rcs = randomChars[rand.Next(0, randomChars.Length)];
            chars.Insert(rand.Next(0, chars.Count), rcs[rand.Next(0, rcs.Length)]);
        }

        return new string(chars.ToArray());
    }
}
