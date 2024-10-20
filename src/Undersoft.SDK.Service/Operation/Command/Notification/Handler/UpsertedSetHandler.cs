using MediatR;

namespace Undersoft.SDK.Service.Operation.Command.Notification.Handler;
using Series;
using Undersoft.SDK.Proxies;
using Undersoft.SDK.Service.Data.Event;
using Undersoft.SDK.Service.Data.Store;
using Undersoft.SDK.Service.Data.Store.Repository;
using Undersoft.SDK.Service.Operation.Command.Notification;

public class UpsertedSetHandler<TStore, TEntity, TDto>
    : INotificationHandler<UpsertedSet<TStore, TEntity, TDto>>
    where TDto : class, IOrigin, IInnerProxy
    where TEntity : class, IOrigin, IInnerProxy
    where TStore : IDataServerStore
{
    protected readonly IStoreRepository<TEntity> _repository;
    protected readonly IStoreRepository<Event> _eventStore;

    public UpsertedSetHandler() { }

    public UpsertedSetHandler(IStoreRepository<IEventStore, Event> eventStore)
    {
        _eventStore = eventStore;
    }

    public UpsertedSetHandler(
        IStoreRepository<IReportStore, TEntity> repository,
        IStoreRepository<IEventStore, Event> eventStore
    )
    {
        _repository = repository;
        _eventStore = eventStore;
    }

    public virtual Task Handle(
        UpsertedSet<TStore, TEntity, TDto> request,
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

        if (_repository == null || request.PublishMode != PublishMode.Propagate)
            return Task.CompletedTask;

        ISeries<TEntity> entities;

        if (request.Conditions != null)
            entities = _repository
                .PutBy(
                    request.Select(d => d.Command.Contract),
                    request.Predicate,
                    request.Conditions
                )
                .ToCatalog();
        else
            entities = _repository
                .PutBy(request.Select(d => d.Command.Contract), request.Predicate)
                .ToCatalog();

        request.ForEach(
            (r) =>
            {
                _ = entities.ContainsKey(r.EntityId)
                    ? r.PublishStatus = PublishStatus.Complete
                    : r.PublishStatus = PublishStatus.Uncomplete;
            }
        );
        return Task.CompletedTask;
    }
}
