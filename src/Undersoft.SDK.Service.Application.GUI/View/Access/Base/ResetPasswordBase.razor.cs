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
using Undersoft.SDK.Service.Application.GUI.View.Generic.Form.Dialog;
using Undersoft.SDK.Updating;

namespace Undersoft.SDK.Service.Application.GUI.View.Access.Base;

/// <summary>
/// The reset password base.
/// </summary>
public partial class ResetPasswordBase<TLogo, TAccount> : ComponentBase where TAccount : class, IAuthorization, new () where TLogo : Icon, new()
{
    /// <summary>
    /// Gets or sets the access.
    /// </summary>
    /// <value>An <see cref="IAccess"/></value>
    [Inject]
    private IAccess _access { get; set; } = default!;

    /// <summary>
    /// Gets or sets the navigation.
    /// </summary>
    /// <value>A <see cref="NavigationManager"/></value>
    [Inject]
    private NavigationManager _navigation { get; set; } = default!;

    /// <summary>
    /// Gets or sets the servicer.
    /// </summary>
    /// <value>An <see cref="IServicer"/></value>
    [Inject]
    private IServicer _servicer { get; set; } = default!;

    /// <summary>
    /// Gets or sets the dialog service.
    /// </summary>
    /// <value>An <see cref="IDialogService"/></value>
    [Inject]
    public IDialogService DialogService { get; set; } = default!;

    /// <summary>
    /// The dialog.
    /// </summary>
    private IViewDialog<Credentials> _dialog = default!;

    /// <summary>
    /// On initialized.
    /// </summary>
    protected override void OnInitialized()
    {
        _dialog = _servicer.Activate<
            AccessDialog<TLogo, GenericFormDialog<Credentials, AccessValidator>, Credentials>
        >(DialogService);
    }

    /// <summary>
    /// On after render.
    /// </summary>
    /// <param name="firstRender">If true, first render.</param>
    /// <returns>A <see cref="Task"/></returns>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
            await ResettingPassword("Reset password", "Please enter registered e-mail address");
    }

    /// <summary>
    /// Resetting the password.
    /// </summary>
    /// <param name="title">The title.</param>
    /// <param name="description">The description.</param>
    /// <returns>A <see cref="Task"/></returns>
    private async Task ResettingPassword(string title, string description = "")
    {
        var data = new ViewData<Credentials>(new Credentials(), OperationKind.Update, title);
        data.SetVisible(nameof(Credentials.Email));
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

    /// <summary>
    /// Handle the dialog.
    /// </summary>
    /// <param name="content">The content.</param>
    /// <returns>A <see cref="Task"/> of type <see cref="IAuthorization"/>
    ///     </returns>
    private async Task<IAuthorization?> HandleDialog(IViewData<Credentials>? content)
    {
        if (content == null)
        {
            _navigation.NavigateTo("");
            return null;
        }

        var result = await _access.ResetPassword(new TAccount() { Credentials = content!.Model });

        if (
            result.Notes.Status != AccessStatus.InvalidEmail
            && result.Notes.Status == AccessStatus.ResetPasswordNotConfirmed
        )
        {
            _navigation.NavigateTo($"access/confirm_password_reset/{result.Credentials.Email}");
            return null;
        }

        return result;
    }
}
