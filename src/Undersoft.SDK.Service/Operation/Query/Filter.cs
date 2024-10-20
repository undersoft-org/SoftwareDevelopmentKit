namespace Undersoft.SDK.Service.Operation.Query;

using Undersoft.SDK.Proxies;
using Undersoft.SDK.Service.Data.Query;
using Undersoft.SDK.Service.Data.Store;

public class Filter<TStore, TEntity, TDto> : Query<TEntity, TDto>
    where TEntity : class, IOrigin, IInnerProxy
    where TStore : IDataServerStore
    where TDto : class, IOrigin, IInnerProxy
{
    public Filter(int offset, int limit, IQueryParameters<TEntity> parameters)
        : base(OperationKind.Filter | OperationKind.Query, parameters)
    {
        Offset = offset;
        Limit = limit;
    }

    public Filter(IQueryParameters<TEntity> parameters) : base(OperationKind.Filter | OperationKind.Query, parameters) { }

    public Filter() : base(OperationKind.Filter | OperationKind.Query) { }

    public Filter(
        int offset,
        int limit) : base(OperationKind.Filter | OperationKind.Query)
    {
        Offset = offset;
        Limit = limit;
    }
}
