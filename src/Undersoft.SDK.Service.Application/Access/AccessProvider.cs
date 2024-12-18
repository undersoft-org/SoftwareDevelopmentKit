﻿using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Security.Claims;
using System.Text.Json;
using Undersoft.SDK.Series;
using Undersoft.SDK.Service.Access;
using Undersoft.SDK.Service.Application.Extensions;
using Undersoft.SDK.Service.Data.Remote.Repository;
using Undersoft.SDK.Service.Data.Store;
using Undersoft.SDK.Service.Operation;
using Undersoft.SDK.Updating;
using Undersoft.SDK.Utilities;
using Claim = System.Security.Claims.Claim;

namespace Undersoft.SDK.Service.Application.Access;

public class AccessProvider<TAccount> : AuthenticationStateProvider, IAccessProvider<TAccount> where TAccount : class, IAuthorization
{
    private readonly IJSRuntime js;
    private IAuthorization _authorization;
    private readonly IRemoteRepository<IAccountStore, TAccount> _repository;
    private readonly string TOKEN_STORAGE = "token";
    private readonly string TOKEN_EXPIRATION_STORAGE = "token_exp";
    private readonly string EMAIL_STORAGE = "email";
    private TAccount? _account;
    private AccessState? _accessState;
    private DateTime? _expiration;
    private bool _refreshing = false;

    private AuthenticationState Anonymous =>
        new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

    public AccessProvider(
        IJSRuntime js,
        IRemoteRepository<IAccountStore, TAccount> repository,
        IAuthorization authorization
    )
    {
        this.js = js;
        _repository = repository;
        _authorization = authorization;
    }

    public IAuthorization Current => _authorization;

    public DateTime? Expiration => _expiration;

    public async override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await js.GetFromLocalStorage(TOKEN_STORAGE);
        var email = await js.GetFromLocalStorage(EMAIL_STORAGE);

        if (string.IsNullOrEmpty(token))
        {
            return Anonymous;
        }
       
        var expirationTimeString = await js.GetFromLocalStorage(TOKEN_EXPIRATION_STORAGE);

        if (expirationTimeString != null)
        {
            DateTimeOffset expirationTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expirationTimeString));

            if (IsExpired(expirationTime.LocalDateTime))
            {
                await CleanUp().ConfigureAwait(false);
                return Anonymous;
            }

            if (IsExpired(expirationTime.LocalDateTime.AddMinutes(-5)))
            {
                var auth = await SignedIn(
                    new Authorization()
                    {
                        Credentials = new Credentials() { Email = email, SessionToken = token }
                    }
                ).ConfigureAwait(false);

                if (auth != null)
                {
                    _authorization.Credentials = auth.Credentials;
                    if (auth.Credentials.SessionToken == null)
                        return Anonymous;
                    else
                        return GetAccessState(auth.Credentials.SessionToken);
                }
                else
                    return Anonymous;                
            }
        }

        _authorization.Credentials.Email = email;
        return await GetAccessStateAsync(token).ConfigureAwait(false);
    }

    public async Task<ClaimsPrincipal?> RefreshAsync()
    {
        ClaimsPrincipal? user = null; 
        if (!_refreshing)
        {
            _refreshing = true;
            user = (await GetAuthenticationStateAsync()).User;
            _refreshing = false;
        }
        return user;
    }

    public AccessState GetAccessState(string token)
    {
        _authorization.Credentials.SessionToken = token;
        _accessState = new AccessState(
            new ClaimsPrincipal(new ClaimsIdentity(GetClaims(token), "jwt", "name", "role"))
        );
        if (_accessState.User.Identity != null)
            _authorization.Credentials.Authenticated = _accessState.User.Identity.IsAuthenticated;
        if (_authorization.Credentials.Authenticated)
            _repository.Context.SetAuthorization(token);
        return _accessState;
    }

    public async Task<AccessState> GetAccessStateAsync(string token)
    {
        _authorization.Credentials.SessionToken = token;
        await Registered(typeof(TAccount).New<TAccount>());
        _accessState = new AccessState(
            new ClaimsPrincipal(new ClaimsIdentity(GetClaims(token), "jwt", "name", "role"))
        );
        if (_accessState.User.Identity != null)
            _authorization.Credentials.Authenticated = _accessState.User.Identity.IsAuthenticated;
        if (_authorization.Credentials.Authenticated)
            _repository.Context.SetAuthorization(token);
        return _accessState;
    }

    private ISeries<Claim> GetClaims(string jwt)
    {
        var claims = new Registry<Claim>();
        var payload = jwt.Split('.')[1];
        var token = DecodeBase64(payload);

        var keyValuePairs = token.FromJson<Dictionary<string, object>>();
        if (keyValuePairs != null)
            keyValuePairs.ForEach(kvp =>
            {
                claims.Add(kvp.Key, new Claim(kvp.Key, kvp.Value.ToString() ?? ""));
            });

        if (claims.TryGet("exp", out Claim expiration))
        {
            this._expiration = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expiration.Value)).LocalDateTime;
            js.SetInLocalStorage(TOKEN_EXPIRATION_STORAGE, expiration.Value);
        }

        return claims;
    }

    private byte[] DecodeBase64(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2:
                base64 += "==";
                break;
            case 3:
                base64 += "=";
                break;
        }
        return Convert.FromBase64String(base64);
    }

    private bool IsExpired(DateTime expirationTime)
    {
        return expirationTime <= DateTime.Now;
    }

    public async Task<IAuthorization?> SignIn(IAuthorization auth)
    {
        var result = await _repository.Service(nameof(SignIn), auth);

        if (result == null)
            return null;

        _authorization.Credentials = result.Credentials;
        _authorization.Notes = result.Notes;
        if (result.Credentials.SessionToken != null)
        {
            await js.SetInLocalStorage(TOKEN_STORAGE, result.Credentials.SessionToken);
            await js.SetInLocalStorage(EMAIL_STORAGE, result.Credentials.Email);
            var authState = await GetAccessStateAsync(result.Credentials.SessionToken);
            NotifyAuthenticationStateChanged(Task.FromResult((AuthenticationState)authState));
        }
        return _authorization;
    }

    public async Task<IAuthorization> SignUp(IAuthorization auth)
    {
        var result = await _repository.Service(nameof(SignUp), auth);
        _authorization.Credentials = result.Credentials;
        _authorization.Notes = result.Notes;
        return result;
    }

    public async Task<IAuthorization> SignOut(IAuthorization auth)
    {
        auth.Credentials.Email = await js.GetFromLocalStorage(EMAIL_STORAGE);

        var result = await _repository.Service(nameof(SignOut), auth);

        await CleanUp();
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
        return result;
    }

    public async Task<IAuthorization?> SignedIn(IAuthorization auth)
    {
        var result = await _repository.Service(nameof(SignedIn), auth);

        if (result == null)
            return null;

        result.PatchTo(_authorization);
        if (result.Credentials.SessionToken != null)
        {
            await js.SetInLocalStorage(TOKEN_STORAGE, result.Credentials.SessionToken);
            await js.SetInLocalStorage(EMAIL_STORAGE, result.Credentials.Email);
            var authState = await GetAccessStateAsync(result.Credentials.SessionToken);
            NotifyAuthenticationStateChanged(Task.FromResult((AuthenticationState)authState));
        }
        return _authorization;
    }

    public async Task<IAuthorization?> ResetPassword(IAuthorization auth)
    {
        var result = await _repository.Service(nameof(ResetPassword), auth);

        if (result == null)
            return null;

        return _authorization = result;
    }

    public async Task<IAuthorization?> ChangePassword(IAuthorization auth)
    {
        var result = await _repository.Service(nameof(ChangePassword), auth);

        if (result == null)
            return null;

        return _authorization = result;
    }

    public async Task<IAuthorization?> SignedUp(IAuthorization auth)
    {
        var result = await _repository.Service(nameof(SignedUp), auth);

        if (result == null)
            return null;

        return result.PatchTo(_authorization);
    }

    public async Task<IAuthorization?> ConfirmEmail(IAuthorization auth)
    {
        var result = await _repository.Service(nameof(ConfirmEmail), auth);

        if (result == null)
            return null;

        return result.PatchTo(_authorization);
    }

    public async Task<TAccount> Register(TAccount auth)
    {
        var result = await _repository.Service(nameof(Register), auth);

        if (result == null)
            return default(TAccount)!;

        ((IAuthorization)result).Credentials.Authenticated = _authorization
            .Credentials
            .Authenticated;
        ((IAuthorization)result).PatchTo(_authorization);

        _account = result;

        return result;
    }

    public async Task<TAccount> Unregister(TAccount auth)
    {
        var result = await _repository.Service(nameof(Unregister), auth);

        if (result == null)
            return default(TAccount)!;

        ((IAuthorization)result).Credentials.Authenticated = _authorization
            .Credentials
            .Authenticated;
        ((IAuthorization)result).PatchTo(_authorization);
        _account = result;

        return result;
    }

    public async Task<TAccount> Registered(TAccount auth)
    {
        if (_account != null)
            return _account;

        auth.Credentials = _authorization.Credentials;

        var result = await _repository.Service(nameof(Registered), auth);

        if (result == null)
            return default(TAccount)!;

        ((IAuthorization)result).Credentials.Authenticated = _authorization
            .Credentials
            .Authenticated;
        ((IAuthorization)result).PatchTo(_authorization);
        _account = result;

        return result;
    }

    private async Task CleanUp()
    {
        var auth = _authorization;
        await js.RemoveItem(TOKEN_STORAGE);
        await js.RemoveItem(TOKEN_EXPIRATION_STORAGE);
        await js.RemoveItem(EMAIL_STORAGE);
        auth.Notes = new OperationNotes();
        auth.Credentials = new Credentials();
        _repository.Context.SetAuthorization(null);
    }
}
