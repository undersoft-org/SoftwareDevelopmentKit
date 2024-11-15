using FluentValidation;
using Microsoft.FluentUI.AspNetCore.Components;
using Undersoft.SDK.Proxies;
using Undersoft.SDK.Service.Access;
using Undersoft.SDK.Service.Application.GUI.Models;
using Undersoft.SDK.Service.Application.GUI.View.Abstraction;

namespace Undersoft.SDK.Service.Application.GUI.View.Generic.Landing
{
    public partial class GenericLandingHeader<TMenu, TAccount> : FluentComponentBase where TMenu : class, IOrigin, IInnerProxy where TAccount : class, IOrigin, IInnerProxy, IAuthorization
    {
        [Inject]
        public NavigationManager? NavigationManager { get; set; }

        [CascadingParameter]
        public AppearanceState? AppearanceState { get; set; } = default!;
    }
}
