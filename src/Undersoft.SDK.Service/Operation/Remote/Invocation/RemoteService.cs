using Undersoft.SDK.Service.Operation.Invocation;

namespace Undersoft.SDK.Service.Operation.Remote.Invocation;

public class RemoteService<TStore, TService, TModel> : Invocation<TModel>
    where TService : class
    where TModel : class
    where TStore : IDataServiceStore
{
    public override OperationSite Site => OperationSite.Client;

    public RemoteService() : base() { }

    public RemoteService(string method, object argument)
        : base(OperationKind.Access, typeof(TService), method, argument) { }

    public RemoteService(string method, Arguments arguments)
        : base(OperationKind.Access, typeof(TService), method, arguments) { }

    public RemoteService(string method, object[] arguments)
        : base(OperationKind.Access, typeof(TService), method, arguments) { }

    public RemoteService(string method, byte[] arguments)
        : base(OperationKind.Access, typeof(TService), method, arguments) { }
}
