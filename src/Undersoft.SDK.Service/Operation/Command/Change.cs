using System.Linq.Expressions;
using System.Text.Json.Serialization;

namespace Undersoft.SDK.Service.Operation.Command;

using Undersoft.SDK;
using Undersoft.SDK.Proxies;
using Undersoft.SDK.Service.Data.Event;
using Undersoft.SDK.Service.Data.Store;

public class Change<TStore, TEntity, TDto> : Command<TDto>
    where TEntity : class, IOrigin, IInnerProxy
    where TDto : class, IOrigin, IInnerProxy
    where TStore : IDataServerStore
{
    [JsonIgnore]
    public Func<TDto, Expression<Func<TEntity, bool>>> Predicate { get; }

    public Change(PublishMode publishMode, TDto input, params object[] keys)
        : base(OperationKind.Change, publishMode, input, keys) { }

    public Change(
        PublishMode publishMode,
        TDto input,
        Func<TDto, Expression<Func<TEntity, bool>>> predicate
    ) : base(OperationKind.Change, publishMode, input)
    {
        Predicate = predicate;
    }
}
