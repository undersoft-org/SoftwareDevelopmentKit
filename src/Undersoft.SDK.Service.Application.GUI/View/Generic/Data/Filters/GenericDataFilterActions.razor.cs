using Undersoft.SDK.Service.Application.GUI.View.Abstraction;

namespace Undersoft.SDK.Service.Application.GUI.View.Generic.Data.Filters;

public partial class GenericDataFilterActions : ViewItem
{
    private Type _type = default!;
    private string? _name { get; set; } = "";
    private string? _label { get; set; }
    private IViewStore? _store;

    [CascadingParameter]
    private bool IsOpen { get; set; }

    [CascadingParameter]
    public bool ShowIcons { get; set; } = true;

    protected override void OnInitialized()
    {
        _type = FilteredType.GetNotNullableType();
        base.OnInitialized();
    }

    [CascadingParameter]
    public Type FilteredType { get; set; } = default!;

    public bool IsAddable => Parent != null ? ((IViewFilter)Parent).IsAddable : false;

    [CascadingParameter]
    public override IViewItem? Root
    {
        get => base.Root;
        set => base.Root = value;
    }

    public bool Added => Parent != null ? ((IViewFilter)Parent).Added : false;

    public void Add()
    {
        if (Parent != null)
        {
            ((IViewFilter)Parent).CloneLast();
        }
    }

    public void Subtract()
    {
        if (Parent != null)
        {
            ((IViewFilter)Parent).RemoveLast();
        }
    }

    public void Close()
    {
        if (Parent != null)
        {
            ((IViewFilter)Parent).Close();
        }
    }

    public void Dismiss()
    {
        if (Parent != null)
        {
            Close();
            ((IViewFilter)Parent).Clear();
        }
    }

    public async Task Apply()
    {
        if (Parent != null)
        {
            Close();
            await ((IViewFilter)Parent).ApplyAsync();
        }
    }
}
