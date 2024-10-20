using MediatR;

namespace Undersoft.SDK.Service.Operation.Command.Notification.Handler;
using Undersoft.SDK.Proxies;
using Undersoft.SDK.Service.Data.Event;
using Undersoft.SDK.Service.Data.Store;
using Undersoft.SDK.Service.Data.Store.Repository;
using Undersoft.SDK.Service.Operation.Command.Notification;

public class CreatedHandler<TStore, TEntity, TDto>
    : INotificationHandler<Created<TStore, TEntity, TDto>>
    where TDto : class, IOrigin, IInnerProxy
    where TEntity : class, IOrigin, IInnerProxy
    where TStore : IDataServerStore
{
    protected readonly IStoreRepository<Event> _eventStore;
    protected readonly IStoreRepository<TEntity> _repository;

    public CreatedHandler() { }

    public CreatedHandler(IStoreRepository<IEventStore, Event> eventStore)
    {
        _eventStore = eventStore;
    }

    public CreatedHandler(
        IStoreRepository<IReportStore, TEntity> repository,
        IStoreRepository<IEventStore, Event> eventStore
    )
    {
        _repository = repository;
        _eventStore = eventStore;
    }

    public virtual Task Handle(
        Created<TStore, TEntity, TDto> request,
        CancellationToken cancellationToken
    )
    {
        if (_eventStore != null)
            _eventStore.Add(request);

        if (_repository == null || request.Command.Mode != PublishMode.Propagate)
            return Task.CompletedTask;

        if (_repository.Add((TEntity)request.Command.Result, request.Predicate) == null)
            throw new Exception(
                $"{$"{GetType().Name} "}{$"for entity {typeof(TEntity).Name} unable create report"}"
            );

        request.PublishStatus = PublishStatus.Complete;

        return Task.CompletedTask;
    }
}
