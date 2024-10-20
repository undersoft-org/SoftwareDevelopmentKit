using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Undersoft.SDK.Service.Data.Event;

namespace Undersoft.SDK.Service.Operation.Command;

public class Update<TStore, TEntity, TDto> : Command<TDto>
    where TEntity : class, IOrigin, IInnerProxy
    where TDto : class, IOrigin, IInnerProxy
    where TStore : IDataServerStore
{
    [JsonIgnore]
    public Func<TDto, Expression<Func<TEntity, bool>>> Predicate { get; }

    [JsonIgnore]
    public Func<TDto, Expression<Func<TEntity, bool>>>[] Conditions { get; }

    public Update(PublishMode publishPattern, TDto input, params object[] keys)
        : base(OperationKind.Update, publishPattern, input, keys) { }

    public Update(
        PublishMode publishPattern,
        TDto input,
        Func<TDto, Expression<Func<TEntity, bool>>> predicate
    ) : base(OperationKind.Update, publishPattern, input)
    {
        Predicate = predicate;
    }

    public Update(
        PublishMode publishPattern,
        TDto input,
        Func<TDto, Expression<Func<TEntity, bool>>> predicate,
        params Func<TDto, Expression<Func<TEntity, bool>>>[] conditions
    ) : base(OperationKind.Update, publishPattern, input)
    {
        Predicate = predicate;
        Conditions = conditions;
    }
}
