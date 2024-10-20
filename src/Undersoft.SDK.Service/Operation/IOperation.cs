using Undersoft.SDK.Service.Data.Event;
using FluentValidation.Results;

namespace Undersoft.SDK.Service
{
    public interface IOperation
    {
        public OperationSite Site { get; }

        public OperationKind Kind { get; }

        public PublishMode Mode { get; }

        public object Input { get; }

        public object Output { get; }

        ValidationResult Validation { get; set; }
    }
}
