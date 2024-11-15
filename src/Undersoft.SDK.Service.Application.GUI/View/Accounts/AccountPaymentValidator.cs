// ********************************************************
//   Copyright (c) Undersoft. All Rights Reserved.
//   Licensed under the MIT License. 
//   author: Dariusz Hanc
//   email: dh@undersoft.pl
//   library: Undersoft.SVC.Service.Application.GUI
// ********************************************************

namespace Undersoft.SDK.Service.Application.GUI.View.Accounts;

using Undersoft.SDK.Service.Access.Licensing;

/// <summary>
/// The account validator.
/// </summary>
public class AccountPaymentValidator<TModel> : ViewValidator<TModel> where TModel : Payment
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AccountValidator"/> class.
    /// </summary>
    /// <param name="servicer">The servicer.</param>
    public AccountPaymentValidator(IServicer servicer) : base(servicer)
    {
        ValidationScope(
            OperationKind.Any,
            () =>
            {
                ValidateRequired(p => p.Model.CardNumber);
                ValidateRequired(p => p.Model.CardCSV);
                ValidateRequired(p => p.Model.CardTitle);
                ValidateRequired(p => p.Model.CardExpirationDate);
                ValidateRequired(p => p.Model.CardType);
            }
        );
    }
}
