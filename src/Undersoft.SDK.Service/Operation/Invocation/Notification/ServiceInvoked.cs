namespace Undersoft.SDK.Service.Operation.Invocation.Notification;

using Undersoft.SDK;
using Undersoft.SDK.Service.Data.Store;
using Undersoft.SDK.Service.Operation.Invocation;

public class ServiceInvoked<TStore, TType, TDto> : InvokeNotification<Invocation<TDto>>
    where TType : class
    where TDto : class, IOrigin
    where TStore : IDataServerStore
{
    public ServiceInvoked(Service<TStore, TType, TDto> command) : base(command)
    {
    }
}
