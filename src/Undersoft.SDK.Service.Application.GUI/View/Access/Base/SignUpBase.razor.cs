using Microsoft.FluentUI.AspNetCore.Components;

// ********************************************************
//   Copyright (c) Undersoft. All Rights Reserved.
//   Licensed under the MIT License.
//   author: Dariusz Hanc
//   email: dh@undersoft.pl
//   library: Undersoft.SVC.Service.Application.GUI
// ********************************************************

using Undersoft.SDK.Service.Access;
using Undersoft.SDK.Service.Application.GUI.View.Abstraction;
using Undersoft.SDK.Service.Application.GUI.View.Access.Dialog;
using Undersoft.SDK.Updating;

namespace Undersoft.SDK.Service.Application.GUI.View.Access.Base;


public partial class SignUpBase<TLogo, TAccount> : ComponentBase where TAccount : class, IAuthorization, new() where TLogo : Icon, new()
{
    [Inject]
    private IAccess _access { get; set; } = default!;

    [Inject]
    private NavigationManager _navigation { get; set; } = default!;

    [Inject]
    private IServicer _servicer { get; set; } = default!;

    [Inject]
    public IDialogService DialogService { get; set; } = default!;

    private IViewDialog<Credentials> _dialog = default!;

    protected override void OnInitialized()
    {
        _dialog = _servicer.Initialize<
            AccessDialog<TLogo, SignUpDialog<Credentials, AccessValidator>, Credentials>
        >(DialogService);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
            await SigningUp("Sign up");
    }

    private async Task SigningUp(string title, string description = "")
    {
        var data = new ViewData<Credentials>(new Credentials(), OperationType.Create, title);
        data.SetVisible(
            nameof(Credentials.FirstName),
            nameof(Credentials.LastName),
            nameof(Credentials.Email),
            nameof(Credentials.Password),
            nameof(Credentials.RetypedPassword)
        );
        data.Description = description;

        while (true)
        {
            await _dialog.Show(data);
            var result = await HandleDialog(_dialog.Content);
            if (result == null)
                break;
            result.Notes.PatchTo(data.Notes);
        }
    }

    private async Task<IAuthorization?> HandleDialog(IViewData<Credentials>? content)
    {
        if (content == null)
        {
            _navigation.NavigateTo("");
            return null;
        }
        if (content!.StateFlags.HaveNext && content.NextHref != null)
        {
            _navigation.NavigateTo(content.NextHref);
            return null;
        }

        content.Model.UserName =
            $"{content.Model.FirstName}__"
            + $"{content.Model.LastName}__{content.Model.Email.ToLowerInvariant().Replace("@", ".")}";

        var result = await _access.SignUp(new TAccount() { Credentials = content.Model });

        if (result.Notes.Status != AccessStatus.Failure && result.Notes.Errors == null)
        {
            _navigation.NavigateTo($"access/confirm_email/{result.Credentials.Email}");
            return null;
        }

        return result;
    }
}
