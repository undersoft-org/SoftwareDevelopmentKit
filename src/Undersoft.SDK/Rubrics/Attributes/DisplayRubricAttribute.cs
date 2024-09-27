namespace Undersoft.SDK.Rubrics.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class DisplayRubricAttribute : RubricAttribute
    {
        public string Name;
        public string Description;
        public string Group;
        public int Order;
        public int Size;
        public string Class;
        public string Style;
        public string Width;
        public string Height;
        public string DataTarget; 
        public string IconMember;
        public ImageMode ImageMode = ImageMode.Regular; 
        public bool Required;
        public bool Disabled;
        public MenuRole MenuRole = MenuRole.Item;
        public string[] FilterMembers;
        public string[] SortMembers;
        public Type FilteredType;
        public string[] AggregateMembers;
        public bool Sortable => SortMembers.Any();
        public bool Filterable => FilterMembers.Any();

        public DisplayRubricAttribute(string name)
        {
            Name = name;
        }

        public string[] QueryMembers
        {
            set
            {
                FilterMembers = value;
                SortMembers = value;
            }
        }
    }

    public enum MenuRole
    {        
        Item,
        Group
    }


    public enum ImageMode
    {
        Regular,
        Persona
    }
}
