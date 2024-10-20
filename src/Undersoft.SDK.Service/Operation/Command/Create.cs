using System.Linq.Expressions;
using System.Text.Json.Serialization;

namespace Undersoft.SDK.Service.Operation.Command;

using Undersoft.SDK;
using Undersoft.SDK.Proxies;
using Undersoft.SDK.Service.Data.Event;
using Undersoft.SDK.Service.Data.Store;

public class Create<TStore, TEntity, TDto> : Command<TDto>
    where TEntity : class, IOrigin, IInnerProxy
    where TDto : class, IOrigin, IInnerProxy
    where TStore : IDataServerStore
{
    [JsonIgnore]
    public Func<TEntity, Expression<Func<TEntity, bool>>> Predicate { get; }

    public Create(PublishMode publishPattern, TDto input)
        : base(OperationKind.Create, publishPattern, input)
    {
        input.AutoId();
    }

    public Create(PublishMode publishPattern, TDto input, object key)
        : base(OperationKind.Create, publishPattern, input)
    {
        input.SetId(key);
    }

    public Create(
        PublishMode publishPattern,
        TDto input,
        Func<TEntity, Expression<Func<TEntity, bool>>> predicate
    ) : base(OperationKind.Create, publishPattern, input)
    {
        input.AutoId();
        Predicate = predicate;
    }
}
