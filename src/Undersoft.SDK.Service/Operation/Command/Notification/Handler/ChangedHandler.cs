using MediatR;

namespace Undersoft.SDK.Service.Operation.Command.Notification.Handler;
using Undersoft.SDK.Proxies;
using Undersoft.SDK.Service.Data.Event;
using Undersoft.SDK.Service.Data.Store;
using Undersoft.SDK.Service.Data.Store.Repository;
using Undersoft.SDK.Service.Operation.Command.Notification;

public class ChangedHandler<TStore, TEntity, TCommand>
    : INotificationHandler<Changed<TStore, TEntity, TCommand>>
    where TEntity : class, IOrigin, IInnerProxy
    where TCommand : class, IOrigin, IInnerProxy
    where TStore : IDataServerStore
{
    protected readonly IStoreRepository<Event> _eventStore;
    protected readonly IStoreRepository<TEntity> _repository;

    public ChangedHandler() { }

    public ChangedHandler(IStoreRepository<IEventStore, Event> eventStore)
    {
        _eventStore = eventStore;
    }

    public ChangedHandler(
        IStoreRepository<IReportStore, TEntity> repository,
        IStoreRepository<IEventStore, Event> eventStore
    )
    {
        _repository = repository;
        _eventStore = eventStore;
    }

    public virtual async Task Handle(
        Changed<TStore, TEntity, TCommand> request,
        CancellationToken cancellationToken
    )
    {
        if (_eventStore != null)
            _eventStore.Add(request);

        if (_repository == null || request.Command.PublishMode != EventPublishMode.PropagateCommand)
            return;

        TEntity entity;
        if (request.Command.Keys != null)
            entity = await _repository.PatchBy(request.Command.Contract, request.Command.Keys);
        else
            entity = await _repository.PatchBy(request.Command.Contract, request.Predicate);

        if (entity == null)
            throw new Exception(
                $"{$"{GetType().Name} for entity "}{$"{typeof(TEntity).Name} unable change report"}"
            );

        request.PublishStatus = EventPublishStatus.Complete;
    }
}
