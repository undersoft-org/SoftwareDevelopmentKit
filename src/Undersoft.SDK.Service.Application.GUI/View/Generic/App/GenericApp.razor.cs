using Undersoft.SDK.Service.Application.GUI.View.Abstraction;

namespace Undersoft.SDK.Service.Application.GUI.View.Generic.App
{
    public partial class GenericApp : ViewItem
    {
        protected override void OnInitialized()
        {
            Data.MapRubrics(t => t.ExtendedRubrics, p => p.Extended);
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
