// ********************************************************
//   Copyright (c) Undersoft. All Rights Reserved.
//   Licensed under the MIT License. 
//   author: Dariusz Hanc
//   email: dh@undersoft.pl
//   library: Undersoft.SVC.Service.Application.GUI
// ********************************************************

using Undersoft.SDK.Service.Access;

namespace Undersoft.SDK.Service.Application.GUI.View.Access;

/// <summary>
/// The access validator.
/// </summary>
public class AccessValidator : ViewValidator<Credentials>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AccessValidator"/> class.
    /// </summary>
    /// <param name="servicer">The servicer.</param>
    public AccessValidator(IServicer servicer) : base(servicer)
    {
        ValidationScope(
            OperationKind.Access | OperationKind.Create | OperationKind.Update,
            () =>
            {
                ValidateEmail(p => p.Model.Email);
                ValidateRequired(p => p.Model.Email);
            }
        );
        ValidationScope(
            OperationKind.Access | OperationKind.Create | OperationKind.Change,
            () =>
            {
                ValidateRequired(p => p.Model.Password);
            }
        );
        ValidationScope(
            OperationKind.Create,
            () =>
            {
                ValidateRequired(p => p.Model.FirstName);
                ValidateRequired(p => p.Model.LastName);
                ValidateEqual(p => p.Model.RetypedPassword, p => p.Model.Password);
            }
        );
        ValidationScope(
            OperationKind.Change,
            () =>
            {
                ValidateRequired(p => p.Model.NewPassword);
                ValidateEqual(p => p.Model.RetypedPassword, p => p.Model.NewPassword);
            }
        );
        ValidationScope(
            OperationKind.Setup,
            () =>
            {
                ValidateRequired(p => p.Model.EmailConfirmationToken);
            }
        );
        ValidationScope(
            OperationKind.Delete,
            () =>
            {
                ValidateRequired(p => p.Model.PasswordResetToken);
            }
        );
    }
}
