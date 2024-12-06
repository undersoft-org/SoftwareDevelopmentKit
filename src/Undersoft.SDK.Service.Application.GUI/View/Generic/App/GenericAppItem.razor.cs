using Microsoft.AspNetCore.Components.Routing;
using Microsoft.FluentUI.AspNetCore.Components;
using Undersoft.SDK.Proxies;
using Undersoft.SDK.Service.Application.GUI.View.Abstraction;
using Undersoft.SDK.Uniques;
using Undersoft.SDK.Utilities;

namespace Undersoft.SDK.Service.Application.GUI.View.Generic.App
{
    public partial class GenericAppItem : ViewItem
    {
        [Inject]
        private NavigationManager _navigation { get; set; } = default!;

        private Type _type = default!;
        private IProxy _proxy = default!;
        private int _index;
        private string? _name { get; set; } = "";
        private string? _label { get; set; }

        protected override void OnInitialized()
        {
            _type = Rubric.RubricType;
            _proxy = Model.Proxy;
            _index = Rubric.RubricId;
            _name = Rubric.RubricName;
            _label = (Rubric.DisplayName != null) ? Rubric.DisplayName : Rubric.RubricName;

            if (Parent != null)
            {
                Id = Rubric.Id.UniqueKey(Parent.Id);
                TypeId = _type.UniqueKey(Parent.TypeId);
            }

            if (Rubric != null && Rubric.IsMenuGroup && _type.IsClass)
            {
                ExtendData = typeof(ViewData<>).MakeGenericType(_type).New<IViewData>(Value);
                ExtendData.MapRubrics(t => t.ExtendedRubrics, p => p.Extended);
                if (Parent != null)
                    Parent.Data.Put(ExtendData);
                Root?.Data.Put(ExtendData);
            }
            base.OnInitialized();
        }

        [CascadingParameter]
        public override IViewItem? Root
        {
            get => base.Root;
            set => base.Root = value;
        }

        private string? Href => GetLinkValue();

        private NavLinkMatch Match => Rubric.PrefixedLink ? NavLinkMatch.Prefix : NavLinkMatch.All;

        public IViewData ExtendData { get; set; } = default!;

        public Icon? GetIconActive()
        {                       
            if (Icon == null) return null;
            var iconType = AssemblyUtilities.FindTypeByFullName(Icon.GetType().FullName!.Replace("Regular", "Filled"));
            if (iconType == null) return null;
            return iconType.New<Icon>();
        }

        private string? GetLinkValue()
        {         
            string? link = null;
            if (Rubric.LinkValue != null)
                link = Rubric.LinkValue;
            else if (Rubric.RubricType == typeof(string))
                link = Value?.ToString();
            return link;
        }

        public void OnClick()
        {
            if (Rubric.Invoker != null)
            {
                Rubric.Invoker.Invoke(Value);
            }
        }
    }
}
