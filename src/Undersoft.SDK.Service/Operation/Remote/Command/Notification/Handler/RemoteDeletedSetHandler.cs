﻿using MediatR;
using Undersoft.SDK.Service.Data.Event;
using Undersoft.SDK.Service.Data.Store.Repository;

namespace Undersoft.SDK.Service.Operation.Remote.Command.Notification.Handler;

public class RemoteDeletedSetHandler<TStore, TDto, TModel>
    : INotificationHandler<RemoteDeletedSet<TStore, TDto, TModel>>
    where TDto : class, IOrigin, IInnerProxy
    where TModel : class, IOrigin, IInnerProxy
    where TStore : IDataServiceStore
{
    protected readonly IStoreRepository<Event> _eventStore;

    public RemoteDeletedSetHandler() { }

    public RemoteDeletedSetHandler(
        IStoreRepository<IEventStore, Event> eventStore
    )
    {
        _eventStore = eventStore;
    }

    public virtual Task Handle(
        RemoteDeletedSet<TStore, TDto, TModel> request,
        CancellationToken cancellationToken
    )
    {
        request.ForOnly(
            d => !d.Command.IsValid,
            d =>
            {
                request.Remove(d);
            }
        );

        if (_eventStore != null)
            _eventStore.Add(request.ForEach(r => r.GetEvent())).Commit();

        return Task.CompletedTask;
    }
}
