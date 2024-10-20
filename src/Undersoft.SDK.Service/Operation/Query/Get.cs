namespace Undersoft.SDK.Service.Operation.Query;

using Undersoft.SDK.Proxies;
using Undersoft.SDK.Service.Data.Query;
using Undersoft.SDK.Service.Data.Store;

public class Get<TStore, TEntity, TDto> : Query<TEntity, TDto>
    where TEntity : class, IOrigin, IInnerProxy
    where TStore : IDataServerStore
    where TDto : class, IOrigin, IInnerProxy
{

    public Get(IQueryParameters<TEntity> parameters = null) : base(OperationKind.Get | OperationKind.Query, parameters) { }

    public Get(
        int offset,
        int limit,
        IQueryParameters<TEntity> parameters = null
    ) : base(OperationKind.Get | OperationKind.Query, parameters)
    {
        Offset = offset;
        Limit = limit;
    }
}
