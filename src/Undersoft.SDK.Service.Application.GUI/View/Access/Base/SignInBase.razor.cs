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


public partial class SignInBase<TLogo, TAccount> : ComponentBase where TAccount : class, IAuthorization, new () where TLogo : Icon, new()
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
            AccessDialog<TLogo, SignInDialog<Credentials, AccessValidator>, Credentials>
        >(DialogService);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
            await SigningIn("Sign in");
    }

    private async Task SigningIn(string title, string description = "")
    {
        var data = new ViewData<Credentials>(new Credentials(), OperationType.Access, title);
        data.SetRequired(nameof(Credentials.Email), nameof(Credentials.Password));
        data.SetVisible(nameof(Credentials.SaveAccountInCookies));
        data.Description = description;

        while (true)
        {
            await _dialog.Show(data);
            var result = await HandleDialog(_dialog.Content);
            if (result == null)
                break;
            data.ClearData();
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

        var result = await _access.SignIn(new TAccount() { Credentials = content!.Model });
        bool adminSignedIn = false;
        if (result.Credentials.Authenticated)
        {
            if (result.Credentials.EmailConfirmed)
                if (result.Credentials.RegistrationCompleted)
                {
                    if (_access != null)
                    {
                        var state = await _access.RefreshAsync();
                        if (state != null)
                        {
                            if (state.IsInRole("Administrator"))
                            {
                                _navigation.NavigateTo("/presenting/admin/dashboard", true);
                                adminSignedIn = true;
                            }
                        }
                    }
                    if (!adminSignedIn)
                        _navigation.NavigateTo("/presenting/user/dashboard", true);
                }
                else
                    _navigation.NavigateTo("/access/register");
            else
                _navigation.NavigateTo($"/access/confirm_email/{result.Credentials.Email}");
            return null;
        }
        return result;
    }
}
