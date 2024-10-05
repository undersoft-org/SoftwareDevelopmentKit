// ********************************************************
//   Copyright (c) Undersoft. All Rights Reserved.
//   Licensed under the MIT License. 
//   author: Dariusz Hanc
//   email: dh@undersoft.pl
//   library: Undersoft.SVC.Service.Application.GUI
// ********************************************************

using Undersoft.SDK.Rubrics.Attributes;
using Undersoft.SDK.Service.Access;
using Undersoft.SDK.Service.Application.GUI.View.Abstraction;
using Undersoft.SDK.Service.Application.GUI.View.Attributes;
using Undersoft.SDK.Service.Data.Object;

namespace Undersoft.SDK.Service.Application.GUI.View.Accounts;
/// <summary>
/// The account menu items.
/// </summary>
public class AccountMenuItems<TAccount> : DataObject where TAccount : class, IOrigin, IAuthorization
{
    /// <summary>
    /// Gets or sets the account.
    /// </summary>
    /// <value>An IViewPanel</value>
    [MenuItem]
    [Extended]
    [DisplayRubric("Account")]
    [Invoke(typeof(AccountPanel), "Open")]
    public IViewPanel<TAccount> Account { get; set; } = default!;

    /// <summary>
    /// Gets or sets the sign up.
    /// </summary>
    /// <value>A <see cref="string"/></value>
    [Link]
    [MenuItem]
    [Extended]
    [DisplayRubric("Sign up")]
    public string SignUp { get; set; } = "/access/sign_up";

    /// <summary>
    /// Gets or sets the sign in.
    /// </summary>
    /// <value>A <see cref="string"/></value>
    [Link]
    [MenuItem]
    [Extended]
    [DisplayRubric("Sign in")]
    public string SignIn { get; set; } = "/access/sign_in";

    /// <summary>
    /// Gets or sets the sign out.
    /// </summary>
    /// <value>A <see cref="string"/></value>
    [Link]
    [MenuItem]
    [Extended]
    [DisplayRubric("Sign out")]
    public string SignOut { get; set; } = "/access/sign_out";
}

