using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Undersoft.SDK.Service.Data.Event;

namespace Undersoft.SDK.Service.Operation.Remote.Command;

public class RemoteChangeSet<TStore, TDto, TModel> : RemoteCommandSet<TModel>
    where TDto : class, IOrigin, IInnerProxy
    where TModel : class, IOrigin, IInnerProxy
    where TStore : IDataServiceStore
{
    [JsonIgnore]
    public Func<TModel, Expression<Func<TDto, bool>>> Predicate { get; }

    public RemoteChangeSet(PublishMode publishPattern, TModel input, object key)
        : base(
            OperationKind.Change,
            publishPattern,
            new[] { new RemoteChange<TStore, TDto, TModel>(publishPattern, input, key) }
        )
    { }

    public RemoteChangeSet(PublishMode publishPattern, TModel[] inputs)
        : base(
            OperationKind.Change,
            publishPattern,
            inputs.Select(c => new RemoteChange<TStore, TDto, TModel>(publishPattern, c, c.Id)).ToArray()
        )
    { }

    public RemoteChangeSet(
        PublishMode publishPattern,
        TModel[] inputs,
        Func<TModel, Expression<Func<TDto, bool>>> predicate
    )
        : base(
            OperationKind.Change,
            publishPattern,
            inputs
                .Select(c => new RemoteChange<TStore, TDto, TModel>(publishPattern, c, predicate))
                .ToArray()
        )
    {
        Predicate = predicate;
    }
}
