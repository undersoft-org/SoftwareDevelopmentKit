using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Undersoft.SDK.Service.Data.Event;

namespace Undersoft.SDK.Service.Operation.Remote.Command;

public class RemoteDelete<TStore, TDto, TModel> : RemoteCommand<TModel>
    where TDto : class, IOrigin, IInnerProxy
    where TModel : class, IOrigin, IInnerProxy
    where TStore : IDataServiceStore
{
    [JsonIgnore]
    public Func<TModel, Expression<Func<TDto, bool>>> Predicate { get; }

    public RemoteDelete(PublishMode publishPattern, TModel input)
        : base(OperationKind.Delete, publishPattern, input) { }

    public RemoteDelete(
        PublishMode publishPattern,
        TModel input,
        Func<TModel, Expression<Func<TDto, bool>>> predicate
    ) : base(OperationKind.Delete, publishPattern, input)
    {
        Predicate = predicate;
    }

    public RemoteDelete(
        PublishMode publishPattern,
        Func<TModel, Expression<Func<TDto, bool>>> predicate
    ) : base(OperationKind.Delete, publishPattern)
    {
        Predicate = predicate;
    }

    public RemoteDelete(PublishMode publishPattern, params object[] keys)
        : base(OperationKind.Delete, publishPattern, keys) { }
}
