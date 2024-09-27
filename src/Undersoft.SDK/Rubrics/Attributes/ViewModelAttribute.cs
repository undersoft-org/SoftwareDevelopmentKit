using Undersoft.SDK.Utilities;

namespace Undersoft.SDK.Rubrics.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ViewModelAttribute : Attribute
    {
        private Type ValidatorType;
        public string Validator;
        public string Width;
        public string Height;
        public string Class;
        public string Style;
        public string[] SearchMembers;

        public ViewModelAttribute()
        {
            if (Validator != null)
            {
                ValidatorType = AssemblyUtilities.FindType(Validator);
                if (ValidatorType == null)
                    ValidatorType = AssemblyUtilities.FindTypeByFullName(Validator);
            }
        }    

        public Type GetValidatorType() => ValidatorType;
    }
}
