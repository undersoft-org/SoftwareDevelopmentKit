using FluentValidation.Results;
using MediatR;
using Undersoft.SDK.Service.Data.Event;
using Undersoft.SDK.Service.Data.Store.Repository;

namespace Undersoft.SDK.Service.Operation.Remote.Command.Notification.Handler;

public class RemoteChangedHandler<TStore, TDto, TCommand>
    : INotificationHandler<RemoteChanged<TStore, TDto, TCommand>>
    where TDto : class, IOrigin, IInnerProxy
    where TCommand : class, IOrigin, IInnerProxy
    where TStore : IDataServiceStore
{
    protected readonly IStoreRepository<Event> _eventStore;

    public RemoteChangedHandler() { }

    public RemoteChangedHandler(
        IStoreRepository<IEventStore, Event> eventStore
    )
    {
        _eventStore = eventStore;
    }

    public virtual Task Handle(
        RemoteChanged<TStore, TDto, TCommand> request,
        CancellationToken cancellationToken
    )
    {
        if (_eventStore != null)
            _eventStore.Add(request);

        return Task.CompletedTask;
    }
}
