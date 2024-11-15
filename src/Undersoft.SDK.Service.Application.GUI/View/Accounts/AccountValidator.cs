// ********************************************************
//   Copyright (c) Undersoft. All Rights Reserved.
//   Licensed under the MIT License. 
//   author: Dariusz Hanc
//   email: dh@undersoft.pl
//   library: Undersoft.SVC.Service.Application.GUI
// ********************************************************

using Undersoft.SDK.Service.Access;
using Undersoft.SDK.Service.Access.Identity;

namespace Undersoft.SDK.Service.Application.GUI.View.Accounts;

public class AccountValidator<TModel> : ViewValidator<TModel> where TModel : class, IOrigin, IAuthorization
{
    public AccountValidator(IServicer servicer) : base(servicer)
    {
        ValidationScope(
            OperationKind.Any,
            () =>
            {
                ValidateEmail(p => ((Personal)p.Model.Proxy["Personal"])!.Email);
                ValidateRequired(p => ((Personal)p.Model.Proxy["Personal"])!.Email);
                ValidateRequired(p => ((Personal)p.Model.Proxy["Personal"])!.PhoneNumber);
                ValidateRequired(p => ((Personal)p.Model.Proxy["Personal"])!.FirstName);
                ValidateRequired(p => ((Personal)p.Model.Proxy["Personal"])!.LastName);
                ValidateRequired(p => ((Address)p.Model.Proxy["Address"])!.Country);
                ValidateRequired(p => ((Address)p.Model.Proxy["Address"])!.City);
                ValidateRequired(p => ((Address)p.Model.Proxy["Address"])!.Postcode);
                ValidateRequired(p => ((Professional)p.Model.Proxy["Professional"])!.Profession);
            }
        );
    }
}
