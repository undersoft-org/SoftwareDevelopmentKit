using Undersoft.SDK.Service.Data.Query;

namespace Undersoft.SDK.Service.Operation.Remote.Query;

public class RemoteFind<TStore, TDto, TModel> : RemoteQuery<TDto, TModel>
    where TModel : class, IOrigin, IInnerProxy
    where TStore : IDataServiceStore
    where TDto : class, IOrigin, IInnerProxy
{
    public RemoteFind(params object[] keys)
        : base(OperationKind.Find | OperationKind.Query | OperationKind.Remote, keys) { }

    public RemoteFind(IQueryParameters<TDto> parameters)
        : base(OperationKind.Find | OperationKind.Query | OperationKind.Remote, parameters) { }

    public RemoteFind() : base(OperationKind.Find | OperationKind.Query | OperationKind.Remote) { }
}
