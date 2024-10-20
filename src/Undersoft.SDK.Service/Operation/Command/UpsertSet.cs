using System.Linq.Expressions;
using System.Text.Json.Serialization;

namespace Undersoft.SDK.Service.Operation.Command;

using Undersoft.SDK;
using Undersoft.SDK.Proxies;
using Undersoft.SDK.Service.Data.Event;
using Undersoft.SDK.Service.Data.Store;

public class UpsertSet<TStore, TEntity, TDto> : CommandSet<TDto>
    where TDto : class, IOrigin, IInnerProxy
    where TEntity : class, IOrigin, IInnerProxy
    where TStore : IDataServerStore
{
    [JsonIgnore]
    public Func<TEntity, Expression<Func<TEntity, bool>>> Predicate { get; }

    [JsonIgnore]
    public Func<TEntity, Expression<Func<TEntity, bool>>>[] Conditions { get; }

    public UpsertSet(PublishMode publishPattern, TDto input, object key)
        : base(
            OperationKind.Change,
            publishPattern,
            new[]
            {
                new Upsert<TStore, TEntity, TDto>(
                    publishPattern,
                    input,
                    e => e => e.Id == (long)key
                )
            }
        )
    { }

    public UpsertSet(
        PublishMode publishPattern,
        TDto[] inputs,
        Func<TEntity, Expression<Func<TEntity, bool>>> predicate
    )
        : base(
            OperationKind.Upsert,
            publishPattern,
            inputs
                .Select(
                    input => new Upsert<TStore, TEntity, TDto>(publishPattern, input, predicate)
                )
                .ToArray()
        )
    {
        Predicate = predicate;
    }

    public UpsertSet(
        PublishMode publishPattern,
        TDto[] inputs,
        Func<TEntity, Expression<Func<TEntity, bool>>> predicate,
        params Func<TEntity, Expression<Func<TEntity, bool>>>[] conditions
    )
        : base(
            OperationKind.Upsert,
            publishPattern,
            inputs
                .Select(
                    input =>
                        new Upsert<TStore, TEntity, TDto>(
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
