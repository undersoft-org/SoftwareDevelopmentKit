using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Undersoft.SDK.Service.Data.Event;

namespace Undersoft.SDK.Service.Operation.Remote.Command;

public class RemoteCreateSet<TStore, TDto, TModel> : RemoteCommandSet<TModel>
    where TModel : class, IOrigin, IInnerProxy
    where TDto : class, IOrigin, IInnerProxy
    where TStore : IDataServiceStore
{
    [JsonIgnore]
    public Func<TDto, Expression<Func<TDto, bool>>> Predicate { get; }

    public RemoteCreateSet(PublishMode publishPattern, TModel input, object key)
        : base(
            OperationKind.Create,
            publishPattern,
            new[] { new RemoteCreate<TStore, TDto, TModel>(publishPattern, input, key) }
        )
    { }

    public RemoteCreateSet(PublishMode publishPattern, TModel[] inputs)
        : base(
            OperationKind.Create,
            publishPattern,
            inputs
                .Select(input => new RemoteCreate<TStore, TDto, TModel>(publishPattern, input))
                .ToArray()
        )
    { }

    public RemoteCreateSet(
        PublishMode publishPattern,
        TModel[] inputs,
        Func<TDto, Expression<Func<TDto, bool>>> predicate
    )
        : base(
            OperationKind.Create,
            publishPattern,
            inputs
                .Select(
                    input => new RemoteCreate<TStore, TDto, TModel>(publishPattern, input, predicate)
                )
                .ToArray()
        )
    {
        Predicate = predicate;
    }
}
