using System.Linq.Expressions;
using System.Text.Json.Serialization;

namespace Undersoft.SDK.Service.Operation.Command;

using Undersoft.SDK;
using Undersoft.SDK.Proxies;
using Undersoft.SDK.Service.Data.Event;
using Undersoft.SDK.Service.Data.Store;

public class ChangeSet<TStore, TEntity, TDto> : CommandSet<TDto>
    where TEntity : class, IOrigin, IInnerProxy
    where TDto : class, IOrigin, IInnerProxy
    where TStore : IDataServerStore
{
    [JsonIgnore]
    public Func<TDto, Expression<Func<TEntity, bool>>> Predicate { get; }

    public ChangeSet(PublishMode publishPattern, TDto input, object key)
        : base(
            OperationKind.Change,
            publishPattern,
            new[] { new Change<TStore, TEntity, TDto>(publishPattern, input, key) }
        )
    { }

    public ChangeSet(PublishMode publishPattern, TDto[] inputs)
        : base(
            OperationKind.Change,
            publishPattern,
            inputs.Select(c => new Change<TStore, TEntity, TDto>(publishPattern, c, c.Id)).ToArray()
        )
    { }

    public ChangeSet(
        PublishMode publishPattern,
        TDto[] inputs,
        Func<TDto, Expression<Func<TEntity, bool>>> predicate
    )
        : base(
            OperationKind.Change,
            publishPattern,
            inputs
                .Select(c => new Change<TStore, TEntity, TDto>(publishPattern, c, predicate))
                .ToArray()
        )
    {
        Predicate = predicate;
    }
}
