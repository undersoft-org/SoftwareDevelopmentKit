using MediatR;

namespace Undersoft.SDK.Service.Operation.Remote.Command.Notification.Handler;
using Logging;
using Undersoft.SDK.Proxies;
using Undersoft.SDK.Service.Data.Event;
using Undersoft.SDK.Service.Data.Store;
using Undersoft.SDK.Service.Data.Store.Repository;
using Undersoft.SDK.Service.Operation.Remote.Command.Notification;

public class RemoteUpsertedSetHandler<TStore, TDto, TModel>
    : INotificationHandler<RemoteUpsertedSet<TStore, TDto, TModel>>
    where TDto : class, IOrigin, IInnerProxy
    where TModel : class, IOrigin, IInnerProxy
    where TStore : IDataServiceStore
{
    protected readonly IStoreRepository<Event> _eventStore;

    public RemoteUpsertedSetHandler() { }

    public RemoteUpsertedSetHandler(
        IStoreRepository<IEventStore, Event> eventStore
    )
    {
        _eventStore = eventStore;
    }

    public virtual Task Handle(
        RemoteUpsertedSet<TStore, TDto, TModel> request,
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
