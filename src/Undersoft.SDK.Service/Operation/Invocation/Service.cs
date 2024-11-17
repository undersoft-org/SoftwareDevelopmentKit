namespace Undersoft.SDK.Service.Operation.Invocation;

using Undersoft.SDK;
using Undersoft.SDK.Service.Data.Store;

public class Service<TStore, TService, TDto> : Invocation<TDto>
    where TDto : class
    where TService : class
    where TStore : IDataServerStore
{
    public override OperationSite Site => OperationSite.Server;

    public Service() : base() { }

    public Service(string method, object argument) : base(OperationKind.Access, typeof(TService), method, argument) { }

    public Service(string method, Arguments arguments)
     : base(OperationKind.Access, typeof(TService), method, arguments) { }

    public Service(string method, params object[] arguments)
    : base(OperationKind.Access, typeof(TService), method, arguments) { }

    public Service(string method, params byte[] arguments)
   : base(OperationKind.Access, typeof(TService), method, arguments) { }

}
