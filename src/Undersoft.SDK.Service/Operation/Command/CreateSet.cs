using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Undersoft.SDK.Service.Data.Event;

namespace Undersoft.SDK.Service.Operation.Command;

public class CreateSet<TStore, TEntity, TDto> : CommandSet<TDto>
    where TEntity : class, IOrigin, IInnerProxy
    where TDto : class, IOrigin, IInnerProxy
    where TStore : IDataServerStore
{
    [JsonIgnore]
    public Func<TEntity, Expression<Func<TEntity, bool>>> Predicate { get; }

    public CreateSet(PublishMode publishPattern, TDto input, object key)
        : base(
            OperationKind.Create,
            publishPattern,
            new[] { new Create<TStore, TEntity, TDto>(publishPattern, input, key) }
        )
    { }

    public CreateSet(PublishMode publishPattern, TDto[] inputs)
        : base(
            OperationKind.Create,
            publishPattern,
            inputs
                .Select(input => new Create<TStore, TEntity, TDto>(publishPattern, input))
                .ToArray()
        )
    { }

    public CreateSet(
        PublishMode publishPattern,
        TDto[] inputs,
        Func<TEntity, Expression<Func<TEntity, bool>>> predicate
    )
        : base(
            OperationKind.Create,
            publishPattern,
            inputs
                .Select(
                    input => new Create<TStore, TEntity, TDto>(publishPattern, input, predicate)
                )
                .ToArray()
        )
    {
        Predicate = predicate;
    }
}
