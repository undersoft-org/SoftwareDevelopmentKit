using MediatR;

namespace Undersoft.SDK.Service.Operation.Command.Notification.Handler;
using Undersoft.SDK.Proxies;
using Undersoft.SDK.Service.Data.Event;
using Undersoft.SDK.Service.Data.Store;
using Undersoft.SDK.Service.Data.Store.Repository;
using Undersoft.SDK.Service.Operation.Command.Notification;

public class DeletedHandler<TStore, TEntity, TDto>
    : INotificationHandler<Deleted<TStore, TEntity, TDto>>
    where TDto : class, IOrigin, IInnerProxy
    where TEntity : class, IOrigin, IInnerProxy
    where TStore : IDataServerStore
{
    protected readonly IStoreRepository<TEntity> _repository;
    protected readonly IStoreRepository<Event> _eventStore;

    public DeletedHandler() { }

    public DeletedHandler(IStoreRepository<IEventStore, Event> eventStore)
    {
        _eventStore = eventStore;
    }

    public DeletedHandler(
        IStoreRepository<IReportStore, TEntity> repository,
        IStoreRepository<IEventStore, Event> eventStore
    )
    {
        _repository = repository;
        _eventStore = eventStore;
    }

    public virtual async Task Handle(
        Deleted<TStore, TEntity, TDto> request,
        CancellationToken cancellationToken
    )
    {
        if (_eventStore != null)
            _eventStore.Add(request);

        if (_repository == null || request.Command.Mode != PublishMode.Propagate)
            return;

        TEntity result = null;
        if (request.Command.Keys != null)
            result = await _repository.Delete(request.Command.Keys);
        else if (request.Data == null && request.Predicate != null)
            result = await _repository.Delete(request.Predicate);
        else
            result = await _repository.DeleteBy(
                request.Command.Contract,
                request.Predicate
            );

        if (result == null)
            throw new Exception(
                $"{GetType().Name} "
                    + $"for entity {typeof(TEntity).Name} unable delete report"
            );

        request.PublishStatus = PublishStatus.Complete;    
    }
}
