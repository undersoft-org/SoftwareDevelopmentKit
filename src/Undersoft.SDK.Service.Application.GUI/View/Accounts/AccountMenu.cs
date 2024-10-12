// ********************************************************
//   Copyright (c) Undersoft. All Rights Reserved.
//   Licensed under the MIT License. 
//   author: Dariusz Hanc
//   email: dh@undersoft.pl
//   library: Undersoft.SVC.Service.Application.GUI
// ********************************************************

using Undersoft.SDK.Rubrics.Attributes;
using Undersoft.SDK.Service.Access;
using Undersoft.SDK.Service.Application.GUI.View.Attributes;
using Undersoft.SDK.Service.Data.Object;

namespace Undersoft.SDK.Service.Application.GUI.View.Accounts;

/// <summary>
/// The account menu.
/// </summary>
public class AccountMenu<TAccount> : DataObject where TAccount : class, IOrigin, IAuthorization
{
    /// <summary>
    /// Gets or sets the account.
    /// </summary>
    /// <value>An <see cref="AccountMenuItems"/></value>
    [MenuGroup]
    [Extended]
    public AccountMenuItems<TAccount> Account { get; set; } = new();
}

