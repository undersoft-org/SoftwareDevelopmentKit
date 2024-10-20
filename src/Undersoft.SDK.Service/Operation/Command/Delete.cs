using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Undersoft.SDK.Service.Data.Event;

namespace Undersoft.SDK.Service.Operation.Command;

public class Delete<TStore, TEntity, TDto> : Command<TDto>
    where TEntity : class, IOrigin, IInnerProxy
    where TDto : class, IOrigin, IInnerProxy
    where TStore : IDataServerStore
{
    [JsonIgnore]
    public Func<TDto, Expression<Func<TEntity, bool>>> Predicate { get; }

    public Delete(PublishMode publishPattern, TDto input)
        : base(OperationKind.Delete, publishPattern, input) { }

    public Delete(
        PublishMode publishPattern,
        TDto input,
        Func<TDto, Expression<Func<TEntity, bool>>> predicate
    ) : base(OperationKind.Delete, publishPattern, input)
    {
        Predicate = predicate;
    }

    public Delete(
        PublishMode publishPattern,
        Func<TDto, Expression<Func<TEntity, bool>>> predicate
    ) : base(OperationKind.Delete, publishPattern)
    {
        Predicate = predicate;
    }

    public Delete(PublishMode publishPattern, params object[] keys)
        : base(OperationKind.Delete, publishPattern, keys) { }
}
