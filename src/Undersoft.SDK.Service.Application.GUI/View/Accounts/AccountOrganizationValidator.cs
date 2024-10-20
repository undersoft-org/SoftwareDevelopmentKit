// ********************************************************
//   Copyright (c) Undersoft. All Rights Reserved.
//   Licensed under the MIT License. 
//   author: Dariusz Hanc
//   email: dh@undersoft.pl
//   library: Undersoft.SVC.Service.Application.GUI
// ********************************************************

using Undersoft.SDK.Service.Access.Identity;

namespace Undersoft.SDK.Service.Application.GUI.View.Accounts;
/// <summary>
/// The account validator.
/// </summary>
public class AccountOrganizationValidator<TModel> : ViewValidator<TModel> where TModel : Organization
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AccountValidator"/> class.
    /// </summary>
    /// <param name="servicer">The servicer.</param>
    public AccountOrganizationValidator(IServicer servicer) : base(servicer)
    {
        ValidationScope(
            OperationKind.Any,
            () =>
            {
                ValidateEmail(p => p.Model.OrganizationEmail);
            }
        );
    }
}
