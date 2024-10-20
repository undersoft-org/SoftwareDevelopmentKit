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
public class AccountProfessionalValidator<TModel> : ViewValidator<TModel> where TModel : Professional
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AccountValidator"/> class.
    /// </summary>
    /// <param name="servicer">The servicer.</param>
    public AccountProfessionalValidator(IServicer servicer) : base(servicer)
    {
        ValidationScope(
            OperationKind.Any,
            () =>
            {
                ValidateEmail(p => p.Model.ProfessionalEmail);
            }
        );
    }
}
