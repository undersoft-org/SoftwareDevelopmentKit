using Microsoft.FluentUI.AspNetCore.Components;
using System.Runtime.CompilerServices;
using Undersoft.SDK.Series;
using Undersoft.SDK.Service.Application.GUI.View.Abstraction;

namespace Undersoft.SDK.Service.Application.GUI.View.Generic.Data.Search
{
    public partial class GenericDataSearchItem : ViewItem
    {
        private Type _type = default!;
        private int _index;
        private string? _name { get; set; } = "";
        private string? _label { get; set; }
        private GenericDataSearch _parent = default!;
        ISeries<Filter> _searchFilters { get; set; } = default!;
        private List<Option<string>>? _operandOptions { get; set; }
        private List<Option<string>>? _linkOptions { get; set; }

        public FluentSearch? FluentSearch { get; set; }

        [CascadingParameter]
        private bool IsOpen { get; set; }

        [CascadingParameter]
        public bool ShowIcons { get; set; } = true;

        [Parameter]
        public override int Index
        {
            get => base.Index;
            set => base.Index = value;
        }

        [Parameter]
        public bool AutoFocus { get; set; }

        protected override void OnInitialized()
        {
            _name = Data.ModelType.Name;
            _label = _name;
            _parent = ((GenericDataSearch)Parent!);
            _searchFilters = Data.SearchFilters ??= new Listing<Filter>();
            base.OnInitialized();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
                await JSRuntime!.InvokeVoidAsync("GenericUtilities.setFocusById", "genericsearchbar");

            await base.OnAfterRenderAsync(firstRender);
        }

        public virtual string? SearchValue { get => Data.SearchValue; set => Data.SearchValue = value; }

        [CascadingParameter]
        public override IViewData Data
        {
            get => base.Data;
            set => base.Data = value;
        }

        [CascadingParameter]
        public override IViewItem? Root
        {
            get => base.Root;
            set => base.Root = value;
        }

        void KeyDownHandlerAsync(FluentKeyCodeEventArgs e)
        {
            if (e.Key == KeyCode.Enter)
                HandleSearchAsync();
        }

        private void HandleSearchAsync()
        {
            if(string.IsNullOrEmpty(SearchValue))
            {
                if (_searchFilters.Any())
                {
                    _searchFilters.Clear();
                    _ = _parent!.LoadViewAsync();
                }
            }
            else if (SearchValue.Length > 2)
            {
                _searchFilters.Clear();
                _searchFilters.Add(SearchValue.Split(' ').SelectMany(w =>
                {
                    return _parent.FilterEntries.ForEach(f => new Filter(
                        f.Member,
                        w.Trim(),
                        CompareOperand.Contains,
                        LinkOperand.And
                    ));                
                }));

                _ = _parent!.LoadViewAsync();
            }
        }

        public async Task LoadViewAsync()
        {
            await ((IViewStore)Parent!).LoadViewAsync();
        }

        private event EventHandler<object> _onMenuItemChange = default!;

        [CascadingParameter]
        public EventHandler<object> OnMenuItemChange
        {
            get => _onMenuItemChange;
            set
            {
                if (value != null)
                    _onMenuItemChange += value;
            }
        }

        public void OnClick()
        {
            if (Rubric.Invoker != null)
            {
                Rubric.Invoker.Invoke(Value);
            }
            else if (OnMenuItemChange != null)
            {
                OnMenuItemChange(this, Data);
            }
        }
    }
}
