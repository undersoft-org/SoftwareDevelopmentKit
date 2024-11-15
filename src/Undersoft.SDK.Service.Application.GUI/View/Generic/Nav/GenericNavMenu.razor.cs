using Undersoft.SDK.Proxies;
using Undersoft.SDK.Series;
using Undersoft.SDK.Service.Application.GUI.View.Abstraction;
using Undersoft.SDK.Utilities;

namespace Undersoft.SDK.Service.Application.GUI.View.Generic.Nav
{
    public partial class GenericNavMenu<TNavMenu> : ViewItem<TNavMenu>, IDisposable where TNavMenu : class, IOrigin, IInnerProxy
    {
        private DotNetObjectReference<GenericNavMenu<TNavMenu>>? _dotNetHelper = null;
        private IJSObjectReference _jsModule = default!;

        protected override void OnInitialized()
        {
            if (Content == null)
                Content = new ViewData<TNavMenu>(typeof(TNavMenu).New<TNavMenu>());

            if (Parent == null)
                Root = this;

            Content.ViewItem = this;

            Children = new Listing<IViewItem>();

            base.OnInitialized();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {

                _jsModule = await JSRuntime!.InvokeAsync<IJSObjectReference>(
                    "import",
                    "./_content/Undersoft.SDK.Service.Application.GUI/View/Generic/Nav/GenericNavMenu.razor.js"
                );

                _dotNetHelper = DotNetObjectReference.Create(this);

                if (BindingId is not null)
                {
                    await _jsModule.InvokeVoidAsync("addEventLeftClick", BindingId, _dotNetHelper);
                }
            }

            await base.OnAfterRenderAsync(firstRender);
        }

        [Parameter]
        public bool SingleMenu { get; set; }

        [Parameter]
        public bool Expanded { get => StateFlags.Expanded; set => StateFlags.Expanded = value; }

        [Parameter]
        public override string? Style { get; set; }

        [Parameter]
        public int? Width { get; set; }

        [Parameter]
        public bool Collapsible { get; set; } = true;

        [Parameter]
        public bool CollapseOnOverlayClick { get; set; } = true;

        [JSInvokable]
        public async Task ToggleExpandedAsync()
        {
            await Task.FromResult(Expanded = !Expanded);
            this.RenderView();
        }

        public async Task SetExpandedAsync(bool value)
        {
            if (value == Expanded)
            {
                return;
            }

            await Task.FromResult(Expanded = value);
        }

        public async Task CollapseAsync()
        {
            await Task.FromResult(Expanded = false);
        }

        public void Dispose()
        {
            _dotNetHelper?.Dispose();
        }
    }
}
