using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Undersoft.SDK.Service.Data.Event;

namespace Undersoft.SDK.Service.Operation.Command;

public class UpdateSet<TStore, TEntity, TDto> : CommandSet<TDto>
    where TEntity : class, IOrigin, IInnerProxy
    where TDto : class, IOrigin, IInnerProxy
    where TStore : IDataServerStore
{
    [JsonIgnore]
    public Func<TDto, Expression<Func<TEntity, bool>>> Predicate { get; }

    [JsonIgnore]
    public Func<TDto, Expression<Func<TEntity, bool>>>[] Conditions { get; }

    public UpdateSet(PublishMode publishPattern) : base(OperationKind.Update)
    {
        Mode = publishPattern;
    }

    public UpdateSet(PublishMode publishPattern, TDto input, object key)
        : base(
            OperationKind.Change,
            publishPattern,
            new[] { new Update<TStore, TEntity, TDto>(publishPattern, input, key) }
        )
    { }

    public UpdateSet(PublishMode publishPattern, TDto[] inputs)
        : base(
            OperationKind.Update,
            publishPattern,
            inputs
                .Select(input => new Update<TStore, TEntity, TDto>(publishPattern, input))
                .ToArray()
        )
    { }

    public UpdateSet(
        PublishMode publishPattern,
        TDto[] inputs,
        Func<TDto, Expression<Func<TEntity, bool>>> predicate
    )
        : base(
            OperationKind.Update,
            publishPattern,
            inputs
                .Select(
                    input => new Update<TStore, TEntity, TDto>(publishPattern, input, predicate)
                )
                .ToArray()
        )
    {
        Predicate = predicate;
    }

    public UpdateSet(
        PublishMode publishPattern,
        TDto[] inputs,
        Func<TDto, Expression<Func<TEntity, bool>>> predicate,
        params Func<TDto, Expression<Func<TEntity, bool>>>[] conditions
    )
        : base(
            OperationKind.Update,
            publishPattern,
            inputs
                .Select(
                    input =>
                        new Update<TStore, TEntity, TDto>(
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
