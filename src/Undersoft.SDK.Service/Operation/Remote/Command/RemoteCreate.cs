using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Undersoft.SDK.Service.Data.Event;

namespace Undersoft.SDK.Service.Operation.Remote.Command;

public class RemoteCreate<TStore, TDto, TModel> : RemoteCommand<TModel>
    where TDto : class, IOrigin, IInnerProxy
    where TModel : class, IOrigin, IInnerProxy
    where TStore : IDataServiceStore
{
    [JsonIgnore]
    public Func<TDto, Expression<Func<TDto, bool>>> Predicate { get; }

    public RemoteCreate(PublishMode publishPattern, TModel input)
        : base(OperationKind.Create, publishPattern, input)
    {
        input.AutoId();
    }

    public RemoteCreate(PublishMode publishPattern, TModel input, object key)
        : base(OperationKind.Create, publishPattern, input)
    {
        input.SetId(key);
    }

    public RemoteCreate(
        PublishMode publishPattern,
        TModel input,
        Func<TDto, Expression<Func<TDto, bool>>> predicate
    ) : base(OperationKind.Create, publishPattern, input)
    {
        input.AutoId();
        Predicate = predicate;
    }
}
