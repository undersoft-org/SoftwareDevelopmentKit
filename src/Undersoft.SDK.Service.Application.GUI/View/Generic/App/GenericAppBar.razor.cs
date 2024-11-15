using Microsoft.FluentUI.AspNetCore.Components;
using Undersoft.SDK.Proxies;
using Undersoft.SDK.Utilities;

namespace Undersoft.SDK.Service.Application.GUI.View.Generic.App
{
    public partial class GenericAppBar<TMenu> : ViewItem<TMenu> where TMenu : class, IOrigin, IInnerProxy
    {

        protected override void OnInitialized()
        {
            if (Content == null)
                Content = new ViewData<TMenu>(typeof(TMenu).New<TMenu>());

            if (Parent == null)
                Root = this;

            Content.ViewItem = this;

            if (SmallIcons)
                Class = "smallicons";

            base.OnInitialized();
        }

        [Parameter]
        public bool SmallIcons { get; set; } = true;

        [Parameter]
        public bool ShowSearch { get; set; } = true;

        [Parameter]
        public Orientation Orientation { get; set; } = Orientation.Horizontal;

        [Parameter]
        public override string? Style { get; set; }

        [Parameter]
        public int? Width { get; set; }
    }
}
