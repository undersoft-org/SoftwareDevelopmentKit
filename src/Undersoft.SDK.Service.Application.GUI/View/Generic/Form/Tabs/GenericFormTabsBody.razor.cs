using FluentValidation;
using Microsoft.FluentUI.AspNetCore.Components;
using Undersoft.SDK.Proxies;
using Undersoft.SDK.Series;
using Undersoft.SDK.Service.Application.GUI.Models;
using Undersoft.SDK.Service.Application.GUI.View.Abstraction;
using Undersoft.SDK.Utilities;

namespace Undersoft.SDK.Service.Application.GUI.View.Generic.Form.Tabs
{
    public partial class GenericFormTabsBody<TModel, TValidator> : GenericDialog<TModel>
        where TValidator : class, IValidator<IViewData<TModel>>
        where TModel : class, IOrigin, IInnerProxy
    {
        [CascadingParameter]
        public override IViewData<TModel> Content { get; set; } = default!;

        private IViewRubrics _rubrics => Content.Rubrics;
        private IViewRubrics _extendedRubrics => Content.ExtendedRubrics;

        private ISeries<IViewData> TabData = new Listing<IViewData>();

        private bool IsDisabled { get; set; }

        public string ActiveId
        {
            get => Content.ActiveRubric!.RubricName;
            set => Content.ActiveRubric = Content.ExtendedRubrics[value];
        }

        [Parameter]
        public override EntryMode EntryMode { get; set; } = EntryMode.Tabs;

        [Parameter]
        public Orientation TabOrientation { get; set; } = Orientation.Vertical;

        [Parameter]
        public Orientation Orientation { get; set; } = Orientation.Vertical;

        protected override void OnInitialized()
        {
            Content.ViewItem = this;
            ResolveEntryMode();
            if (Content.RubricsEnabled)
                Content.MapRubrics(t => t.Rubrics, p => p.Visible);
            if (Content.ExtendedRubricsEnabled)
            {
                Content.MapRubrics(t => t.ExtendedRubrics, p => p.Extended);
                Content.InstantiateNulls(t => t.ExtendedRubrics);
                var firstRubric = Content.ExtendedRubrics.FirstOrDefault();
                if (firstRubric != null)
                    Content.ActiveRubric = firstRubric;
            }
        }

        private IViewData GetTabData(IViewRubric rubric)
        {
            if (!TabData.TryGet(rubric.Id, out IViewData data))
            {
                data = typeof(ViewData<>).MakeGenericType(rubric.RubricType).New<IViewData>(Model.Proxy[rubric.RubricId]);
                data.Title = (rubric.DisplayName != null) ? rubric.DisplayName : rubric.RubricName;
                data.MapRubrics(t => t.Rubrics, p => p.Visible);
                TabData.Add(rubric.Id, data);
            }
            if (IsDisabled != rubric.Disabled)
            {
                bool disabled = rubric.Disabled;
                data.Rubrics.ForEach(r => r.Disabled = disabled).Commit();
                IsDisabled = disabled;
            }
            return data;
        }

        private bool ContainsTabData(IViewRubric rubric)
        {
            return rubric.RubricType.IsClass ? true : false;
        }

        private void ResolveEntryMode()
        {
            if (EntryMode != EntryMode.FormTabs)
            {
                if (EntryMode == EntryMode.Form)
                {
                    Content.RubricsEnabled = true;
                    Content.ExtendedRubricsEnabled = false;
                }
                else if (EntryMode == EntryMode.Tabs)
                {
                    Content.RubricsEnabled = false;
                    Content.ExtendedRubricsEnabled = true;
                }
            }
            else
            {
                TabOrientation = Orientation.Horizontal;
                Content.RubricsEnabled = true;
                Content.ExtendedRubricsEnabled = true;
            }
        }
    }
}