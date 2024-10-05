// ********************************************************
//   Copyright (c) Undersoft. All Rights Reserved.
//   Licensed under the MIT License. 
//   author: Dariusz Hanc
//   email: dh@undersoft.pl
//   library: Undersoft.SVC.Service.Application.GUI
// ********************************************************

using Undersoft.SDK.Service.Access.Identity;

namespace Undersoft.SDK.Service.Application.GUI.View.Accounts;

public class AccountAddressValidator<TModel> : ViewValidator<TModel> where TModel : Address
{
    public AccountAddressValidator(IServicer servicer) : base(servicer)
    {
        ValidationScope(
            OperationType.Any,
            () =>
            {
                ValidateRequired(p => p.Model.Country);
                ValidateRequired(p => p.Model.City);
                ValidateRequired(p => p.Model.Postcode);
                ValidateRequired(p => p.Model.Street);
                ValidateRequired(p => p.Model.Building);
                ValidateRequired(p => p.Model.Apartment);
            }
        );
    }
}
