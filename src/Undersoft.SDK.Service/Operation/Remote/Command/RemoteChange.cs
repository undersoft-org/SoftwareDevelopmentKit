using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Undersoft.SDK.Service.Data.Event;

namespace Undersoft.SDK.Service.Operation.Remote.Command;

public class RemoteChange<TStore, TDto, TModel> : RemoteCommand<TModel>
    where TDto : class, IOrigin, IInnerProxy
    where TModel : class, IOrigin, IInnerProxy
    where TStore : IDataServiceStore
{
    [JsonIgnore]
    public Func<TModel, Expression<Func<TDto, bool>>> Predicate { get; }

    public RemoteChange(PublishMode publishMode, TModel input, params object[] keys)
        : base(OperationKind.Change, publishMode, input, keys) { }

    public RemoteChange(
        PublishMode publishMode,
        TModel input,
        Func<TModel, Expression<Func<TDto, bool>>> predicate
    ) : base(OperationKind.Change, publishMode, input)
    {
        Predicate = predicate;
    }
}
