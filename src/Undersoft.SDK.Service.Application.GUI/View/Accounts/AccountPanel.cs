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

public class AccountPanel
{
    public AccountPanel() { }

    public virtual async Task Open(IViewPanel panel)
    {
        IViewData data;
        if (panel.Content != null)
        {
            data = panel.Content;       

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
    }

    /// <summary>
    /// </summary>
    /// <param name="result"></param>
    /// <TODO> Handle saving account panel</TODO>
    public virtual void HandlePanel(IViewPanel panel)
    {
        if (panel.Content != null && panel.Reference != null)
        {
            dynamic reference = panel.Reference;
            reference.Access.Register(
                panel.Content.Model
            );
        }            
    }
}
