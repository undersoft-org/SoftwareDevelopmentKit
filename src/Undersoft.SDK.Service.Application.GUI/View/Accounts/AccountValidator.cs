// ********************************************************
//   Copyright (c) Undersoft. All Rights Reserved.
//   Licensed under the MIT License. 
//   author: Dariusz Hanc
//   email: dh@undersoft.pl
//   library: Undersoft.SVC.Service.Application.GUI
// ********************************************************

using Undersoft.SDK.Service.Access;

namespace Undersoft.SDK.Service.Application.GUI.View.Accounts;

public class AccountValidator<TModel> : ViewValidator<TModel> where TModel : class, IOrigin, IAuthorization
{
    public AccountValidator(IServicer servicer) : base(servicer)
    {
        ValidationScope(
            OperationKind.Any,
            () =>
            {
            }
        );
    }
}
