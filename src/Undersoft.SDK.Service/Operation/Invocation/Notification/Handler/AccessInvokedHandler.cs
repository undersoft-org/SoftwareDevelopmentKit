﻿using MediatR;

namespace Undersoft.SDK.Service.Operation.Invocation.Notification.Handler;

using Undersoft.SDK;
using Undersoft.SDK.Service.Data.Event;
using Undersoft.SDK.Service.Data.Store;
using Undersoft.SDK.Service.Data.Store.Repository;
using Undersoft.SDK.Service.Operation.Invocation.Notification;

public class AccessInvokedHandler<TStore, TType, TDto>
    : INotificationHandler<AccessInvoked<TStore, TType, TDto>>
    where TType : class
    where TDto : class, IOrigin
    where TStore : IDataStore
{
    protected readonly IStoreRepository<Event> _eventStore;

    public AccessInvokedHandler() { }

    public AccessInvokedHandler(
        IStoreRepository<IEventStore, Event> eventStore
    )
    {
        _eventStore = eventStore;
    }

    public virtual Task Handle(
        AccessInvoked<TStore, TType, TDto> request,
        CancellationToken cancellationToken
    )
    {

        if (_eventStore != null)
            _eventStore.Add(request);
        
        return Task.CompletedTask;
    }
}
