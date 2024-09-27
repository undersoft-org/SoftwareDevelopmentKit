namespace Undersoft.SDK.Rubrics.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ViewRubricAttribute : DisplayRubricAttribute
    {
        public ViewRubricAttribute(string name) : base(name) { }    
    }
}
