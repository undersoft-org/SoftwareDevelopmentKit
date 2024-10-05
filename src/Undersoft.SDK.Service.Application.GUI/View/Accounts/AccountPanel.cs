using Microsoft.FluentUI.AspNetCore.Components;

// ********************************************************
//   Copyright (c) Undersoft. All Rights Reserved.
//   Licensed under the MIT License.
//   author: Dariusz Hanc
//   email: dh@undersoft.pl
//   library: Undersoft.SVC.Service.Application.GUI
// ********************************************************

using Undersoft.SDK.Service.Application.GUI.Models;
using Undersoft.SDK.Service.Application.GUI.View.Abstraction;

namespace Undersoft.SDK.Service.Application.GUI.View.Accounts;

using Undersoft.SDK.Service.Access;
using Undersoft.SDK.Service.Application.GUI.View.Generic.Accounts;

public class AccountPanel<TAccount> where TAccount : class, IOrigin, IAuthorization, new()
{
    public AccountPanel() { }

    public async Task Open(IViewPanel<TAccount> panel)
    {
        IViewData<TAccount> data;
        if (panel.Content != null)
            data = panel.Content;
        else
            data = new ViewData<TAccount>(new TAccount(), OperationType.Any);

        data.EntryMode = EntryMode.Tabs;
        data.Width = "390px";

        await panel.Show(
            data,
            (p) =>
            {
                p.Alignment = HorizontalAlignment.Right;
                p.Title = $"Account";
                p.PrimaryAction = "Ok";
                p.SecondaryAction = null;
                p.ShowDismiss = true;
            }
        );

        HandlePanel(panel);
    }

    /// <summary>
    /// </summary>
    /// <param name="result"></param>
    /// <TODO> Handle saving account panel</TODO>
    public void HandlePanel(IViewPanel<TAccount> panel)
    {
        if (panel.Content != null && panel.Reference != null)
            ((GenericAccountPanel<TAccount, AccountValidator<TAccount>>)panel.Reference).Access.Register(
                panel.Content.Model
            );
    }
}
