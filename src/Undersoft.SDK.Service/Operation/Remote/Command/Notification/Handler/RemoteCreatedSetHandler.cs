using MediatR;
using Undersoft.SDK.Service.Data.Event;
using Undersoft.SDK.Service.Data.Store.Repository;

namespace Undersoft.SDK.Service.Operation.Remote.Command.Notification.Handler;

public class RemoteCreatedSetHandler<TStore, TDto, TModel>
    : INotificationHandler<RemoteCreatedSet<TStore, TDto, TModel>>
    where TDto : class, IOrigin, IInnerProxy
    where TModel : class, IOrigin, IInnerProxy
    where TStore : IDataServiceStore
{
    protected readonly IStoreRepository<Event> _eventStore;

    public RemoteCreatedSetHandler() { }

    public RemoteCreatedSetHandler(
        IStoreRepository<IEventStore, Event> eventStore
    )
    {
        _eventStore = eventStore;
    }

    public virtual Task Handle(
        RemoteCreatedSet<TStore, TDto, TModel> request,
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
