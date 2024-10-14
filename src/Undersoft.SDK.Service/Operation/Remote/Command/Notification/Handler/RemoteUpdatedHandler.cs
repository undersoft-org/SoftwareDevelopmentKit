using FluentValidation.Results;
using MediatR;
using Undersoft.SDK.Service.Data.Event;
using Undersoft.SDK.Service.Data.Store.Repository;

namespace Undersoft.SDK.Service.Operation.Remote.Command.Notification.Handler;

public class RemoteUpdatedHandler<TStore, TDto, TModel>
    : INotificationHandler<RemoteUpdated<TStore, TDto, TModel>>
    where TDto : class, IOrigin, IInnerProxy
    where TModel : class, IOrigin, IInnerProxy
    where TStore : IDataServiceStore
{
    protected readonly IStoreRepository<Event> _eventStore;

    public RemoteUpdatedHandler() { }

    public RemoteUpdatedHandler(
        IStoreRepository<IEventStore, Event> eventStore
    )
    {
        _eventStore = eventStore;
    }

    public virtual Task Handle(
        RemoteUpdated<TStore, TDto, TModel> request,
        CancellationToken cancellationToken
    )
    {
        if (_eventStore != null)
            _eventStore.Add(request);

        return Task.CompletedTask;
    }
}
