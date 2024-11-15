using Microsoft.FluentUI.AspNetCore.Components;
using Undersoft.SDK.Proxies;
using Undersoft.SDK.Service.Access;
using Undersoft.SDK.Service.Application.GUI.Models;

namespace Undersoft.SDK.Service.Application.GUI.View.Generic.Presenting
{
    public partial class GenericPresentingHeader<TNavMenu, TAccount>
        : FluentComponentBase
        where TNavMenu : class, IOrigin, IInnerProxy        
        where TAccount : class, IOrigin, IInnerProxy, IAuthorization
    {
        [Inject]
        private AppearanceState appearance { get; set; } = default!;

        private IJSObjectReference _jsModule = default!;

        [Inject]
        private IJSRuntime JSRuntime { get; set; } = default!;

        [Parameter]
        public Icon? Logo { get; set; }

        [CascadingParameter]
        public AppearanceState? AppearanceState { get; set; } = default!;

        public async Task OnSearchClick()
        {
            _jsModule = await JSRuntime.InvokeAsync<IJSObjectReference>(
                "import",
                "./_content/Undersoft.SDK.Service.Application.GUI/View/Generic/Data/Search/GenericDataSearchItem.razor.js"
            );

            await _jsModule.InvokeVoidAsync("focusElement", "fluentsearchbar");
        }
    }
}
