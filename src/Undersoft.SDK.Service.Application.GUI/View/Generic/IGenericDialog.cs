using Microsoft.FluentUI.AspNetCore.Components;
using Undersoft.SDK.Service.Application.GUI.View.Abstraction;

namespace Undersoft.SDK.Service.Application.GUI.View.Generic
{
    public interface IGenericDialog : IViewItem
    {
        FluentDialog Dialog { get; set; }
        IViewItem Form { get; set; }
    }
}