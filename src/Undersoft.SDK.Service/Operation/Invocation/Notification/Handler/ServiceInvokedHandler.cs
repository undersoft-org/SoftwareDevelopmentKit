using MediatR;

namespace Undersoft.SDK.Service.Operation.Invocation.Notification.Handler;

using Undersoft.SDK;
using Undersoft.SDK.Service.Data.Event;
using Undersoft.SDK.Service.Data.Store;
using Undersoft.SDK.Service.Data.Store.Repository;
using Undersoft.SDK.Service.Operation.Invocation.Notification;

public class ServiceInvokedHandler<TStore, TType, TDto>
    : INotificationHandler<ServiceInvoked<TStore, TType, TDto>>
    where TType : class
    where TDto : class, IOrigin
    where TStore : IDataStore
{
    protected readonly IStoreRepository<Event> _eventStore;

    public ServiceInvokedHandler() { }

    public ServiceInvokedHandler(
        IStoreRepository<IEventStore, Event> eventStore
    )
    {
        _eventStore = eventStore;
    }

    public virtual Task Handle(
        ServiceInvoked<TStore, TType, TDto> request,
        CancellationToken cancellationToken
    )
    {

        if (_eventStore != null)
            _eventStore.Add(request);
        
        return Task.CompletedTask;
    }
}
