using Undersoft.SDK.Utilities;

namespace Undersoft.SDK.Service.Operation
{

    [AttributeUsage(AttributeTargets.Class)]
    public class ValidatorAttribute : Attribute
    {
        public ValidatorAttribute() { }

        public ValidatorAttribute(Type validatorType)
        {
            ValidatorType = validatorType;
            ValidatorTypeName = validatorType.FullName;
        }

        public ValidatorAttribute(string validatorTypeName)
        {
            ValidatorType = AssemblyUtilities.FindType(validatorTypeName);
            ValidatorTypeName = ValidatorType.FullName;
        }

        public ValidatorAttribute(string validatorTypeName, string genericArgumentTypeName)
        {
            var genericValidatorType = AssemblyUtilities.FindGenericType(validatorTypeName, '1');
            var genericValidatorArgumentType = AssemblyUtilities.FindType(genericArgumentTypeName);
            if (genericValidatorType != null && genericValidatorArgumentType != null)
            {
                ValidatorType = genericValidatorType.MakeGenericType(genericValidatorArgumentType);
                ValidatorTypeName = ValidatorType.FullName;
            }
        }

        public ValidatorAttribute(string validatorTypeName, Type genericArgumentType)
        {
            var genericValidatorType = AssemblyUtilities.FindGenericType(validatorTypeName, '1');
            if (genericValidatorType != null && genericArgumentType != null)
            {
                ValidatorType = genericValidatorType.MakeGenericType(genericArgumentType);
                ValidatorTypeName = ValidatorType.FullName;
            }
        }

        public Type ValidatorType { get; set; }

        public string ValidatorTypeName { get; set; }
    }
}
