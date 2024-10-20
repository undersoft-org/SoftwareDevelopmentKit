using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Undersoft.SDK.Service.Data.Event;

namespace Undersoft.SDK.Service.Operation.Remote.Command;

public class RemoteUpdateSet<TStore, TDto, TModel> : RemoteCommandSet<TModel>
    where TDto : class, IOrigin, IInnerProxy
    where TModel : class, IOrigin, IInnerProxy
    where TStore : IDataServiceStore
{
    [JsonIgnore]
    public Func<TModel, Expression<Func<TDto, bool>>> Predicate { get; }

    [JsonIgnore]
    public Func<TModel, Expression<Func<TDto, bool>>>[] Conditions { get; }

    public RemoteUpdateSet(PublishMode publishPattern, TModel input, object key)
        : base(
            OperationKind.Change,
            publishPattern,
            new[] { new RemoteUpdate<TStore, TDto, TModel>(publishPattern, input, key) }
        )
    { }

    public RemoteUpdateSet(PublishMode publishPattern, TModel[] inputs)
        : base(
            OperationKind.Update,
            publishPattern,
            inputs
                .Select(input => new RemoteUpdate<TStore, TDto, TModel>(publishPattern, input))
                .ToArray()
        )
    { }

    public RemoteUpdateSet(
        PublishMode publishPattern,
        TModel[] inputs,
        Func<TModel, Expression<Func<TDto, bool>>> predicate
    )
        : base(
            OperationKind.Update,
            publishPattern,
            inputs
                .Select(
                    input =>
                        new RemoteUpdate<TStore, TDto, TModel>(publishPattern, input, predicate)
                )
                .ToArray()
        )
    {
        Predicate = predicate;
    }

    public RemoteUpdateSet(
        PublishMode publishPattern,
        TModel[] inputs,
        Func<TModel, Expression<Func<TDto, bool>>> predicate,
        params Func<TModel, Expression<Func<TDto, bool>>>[] conditions
    )
        : base(
            OperationKind.Update,
            publishPattern,
            inputs
                .Select(
                    input =>
                        new RemoteUpdate<TStore, TDto, TModel>(
                            publishPattern,
                            input,
                            predicate,
                            conditions
                        )
                )
                .ToArray()
        )
    {
        Predicate = predicate;
        Conditions = conditions;
    }
}
