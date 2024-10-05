// ********************************************************
//   Copyright (c) Undersoft. All Rights Reserved.
//   Licensed under the MIT License. 
//   author: Dariusz Hanc
//   email: dh@undersoft.pl
//   library: Undersoft.SVC.Service.Application.GUI
// ********************************************************

using Undersoft.SDK.Service.Access;

namespace Undersoft.SDK.Service.Application.GUI.View.Access.Base
{
    public partial class SignOutBase : ComponentBase
    {
        [Inject]
        private IAuthorization _auth { get; set; } = default!;

        [Inject] IAccess _access { get; set; } = default!;

        [Inject]
        private NavigationManager _navigation { get; set; } = default!;

        protected async override Task OnInitializedAsync()
        {
            await _access.SignOut(_auth);
            _navigation.NavigateTo("", true);
        }
    }
}