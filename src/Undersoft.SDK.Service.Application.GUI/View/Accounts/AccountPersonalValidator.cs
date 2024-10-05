// ********************************************************
//   Copyright (c) Undersoft. All Rights Reserved.
//   Licensed under the MIT License. 
//   author: Dariusz Hanc
//   email: dh@undersoft.pl
//   library: Undersoft.SVC.Service.Application.GUI
// ********************************************************

namespace Undersoft.SDK.Service.Application.GUI.View.Accounts;

using Undersoft.SDK.Service.Access.Identity;

/// <summary>
/// The account validator.
/// </summary>
public class AccountPersonalValidator<TModel> : ViewValidator<TModel> where TModel : Personal
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AccountValidator"/> class.
    /// </summary>
    /// <param name="servicer">The servicer.</param>
    public AccountPersonalValidator(IServicer servicer) : base(servicer)
    {
        ValidationScope(
            OperationType.Any,
            () =>
            {
                ValidateEmail(p => p.Model.Email);
                ValidateRequired(p => p.Model.Email);
                ValidateRequired(p => p.Model.PhoneNumber);
                ValidateRequired(p => p.Model.FirstName);
                ValidateRequired(p => p.Model.LastName);
            }
        );
    }
}
