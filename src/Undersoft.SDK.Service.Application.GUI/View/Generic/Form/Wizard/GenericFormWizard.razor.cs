using FluentValidation;
using Microsoft.FluentUI.AspNetCore.Components;
using Undersoft.SDK.Proxies;
using Undersoft.SDK.Series;
using Undersoft.SDK.Service.Application.GUI.View.Abstraction;
using Undersoft.SDK.Utilities;

namespace Undersoft.SDK.Service.Application.GUI.View.Generic.Form.Wizard
{
    public partial class GenericFormWizard<TModel, TValidator> : GenericDialog<TModel>, IDialogContentComponent<IViewData<TModel>>
        where TValidator : class, IValidator<IViewData<TModel>>
        where TModel : class, IOrigin, IInnerProxy
    {
        private IViewRubrics _extendedRubrics => Content.ExtendedRubrics;

        private ISeries<IViewData> StepData = new Listing<IViewData>();

        public int ActiveId
        {
            get => Content.ActiveRubric!.RubricOrdinal;
            set => Content.ActiveRubric = Content.ExtendedRubrics[value];
        }

        [Parameter]
        public string? StepStyle { get; set; } = "column-gap:0px;";

        [Parameter]
        public string? StepperSize { get; set; } = "auto";

        [Parameter]
        public Icon? IconCurrent { get; set; } = new Icons.Filled.Size20.Square();

        [Parameter]
        public Icon? IconNext { get; set; } = new Icons.Regular.Size20.Square();

        [Parameter]
        public Icon? IconPrevious { get; set; } = new Icons.Filled.Size20.CheckmarkSquare();

        [Parameter]
        public bool GoToFirstEnabled { get; set; }

        [Parameter]
        public bool GoToLastEnabled { get; set; }

        [Parameter]
        public string? Height { get; set; }

        [Parameter]
        public string? Width { get; set; }

        [Parameter]
        public StepperPosition Position { get; set; }

        protected override void OnInitialized()
        {
            Content.View = this;
            Content.MapRubrics(t => t.ExtendedRubrics, p => p.Extended);
            Content.InstantiateNulls(t => t.ExtendedRubrics);
            var firstRubric = Content.ExtendedRubrics.FirstOrDefault();
            if (firstRubric != null)
                Content.ActiveRubric = firstRubric;

            base.OnInitialized();
        }

        private IViewData GetStepData(IViewRubric rubric)
        {
            if (!StepData.TryGet(rubric.Id, out IViewData data))
            {
                data = typeof(ViewData<>).MakeGenericType(rubric.RubricType).New<IViewData>(Model.Proxy[rubric.RubricId]);
                data.Title = (rubric.DisplayName != null) ? rubric.DisplayName : rubric.RubricName;
                data.MapRubrics(t => t.Rubrics, p => p.Visible);
                StepData.Add(rubric.Id, data);
            }
            return data;
        }

        private bool ContainsStepData(IViewRubric rubric)
        {
            return Model.Proxy[rubric.RubricId] != null;
        }
    }
}