using Undersoft.SDK.Service.Application.GUI.View.Abstraction;

namespace Undersoft.SDK.Service.Application.GUI.View.Generic.App
{
    public partial class GenericApp : ViewItem
    {
        protected override void OnInitialized()
        {
            Data.MapRubrics(t => t.Rubrics, p => p.Visible);
            if (Parent == null)
                Root = this;

            base.OnInitialized();
        }

        [CascadingParameter]
        public override IViewItem? Root
        {
            get => base.Root;
            set => base.Root = value;
        }
    }
}
