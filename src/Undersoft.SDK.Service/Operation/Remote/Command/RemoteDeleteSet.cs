using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Undersoft.SDK.Service.Data.Event;

namespace Undersoft.SDK.Service.Operation.Remote.Command;

public class RemoteDeleteSet<TStore, TDto, TModel> : RemoteCommandSet<TModel>
    where TDto : class, IOrigin, IInnerProxy
    where TModel : class, IOrigin, IInnerProxy
    where TStore : IDataServiceStore
{
    [JsonIgnore]
    public Func<TModel, Expression<Func<TDto, bool>>> Predicate { get; }

    public RemoteDeleteSet(PublishMode publishPattern, object key)
        : base(
            OperationKind.Create,
            publishPattern,
            new[] { new RemoteDelete<TStore, TDto, TModel>(publishPattern, key) }
        )
    { }

    public RemoteDeleteSet(PublishMode publishPattern, TModel input, object key)
        : base(
            OperationKind.Create,
            publishPattern,
            new[] { new RemoteDelete<TStore, TDto, TModel>(publishPattern, input, key) }
        )
    { }

    public RemoteDeleteSet(PublishMode publishPattern, TModel[] inputs)
        : base(
            OperationKind.Delete,
            publishPattern,
            inputs
                .Select(input => new RemoteDelete<TStore, TDto, TModel>(publishPattern, input))
                .ToArray()
        )
    { }

    public RemoteDeleteSet(
        PublishMode publishPattern,
        TModel[] inputs,
        Func<TModel, Expression<Func<TDto, bool>>> predicate
    )
        : base(
            OperationKind.Delete,
            publishPattern,
            inputs
                .Select(
                    input => new RemoteDelete<TStore, TDto, TModel>(publishPattern, input, predicate)
                )
                .ToArray()
        )
    {
        Predicate = predicate;
    }
}
