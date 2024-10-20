namespace Undersoft.SDK.Service.Operation.Invocation;

using Undersoft.SDK;
using Undersoft.SDK.Service.Data.Store;

public class Setup<TStore, TService, TDto> : Invocation<TDto>
    where TDto : class
    where TService : class
    where TStore : IDataServerStore
{
    public override OperationSite Site => OperationSite.Server;

    public Setup() : base() { }

    public Setup(string method, object argument) : base(OperationKind.Action, typeof(TService), method, argument) { }

    public Setup(string method, Arguments arguments)
     : base(OperationKind.Action, typeof(TService), method, arguments) { }

    public Setup(string method, params object[] arguments)
    : base(OperationKind.Action, typeof(TService), method, arguments) { }

    public Setup(string method, params byte[] arguments)
   : base(OperationKind.Action, typeof(TService), method, arguments) { }

}
