using Microsoft.FluentUI.AspNetCore.Components;

namespace Undersoft.SDK.Service.Application.GUI.View.Generic.Landing
{
    public partial class GenericLandingBody : FluentComponentBase
    {
        private GenericPageContents? _toc = null;

        [CascadingParameter]
        public RenderFragment? Body { get; set; }

        public EventCallback OnRefreshTableOfContents => EventCallback.Factory.Create(this, RefreshTableOfContentsAsync);

        private async Task RefreshTableOfContentsAsync()
        {
            if(_toc != null)
                await _toc.RefreshAsync();
        }
    }
}
