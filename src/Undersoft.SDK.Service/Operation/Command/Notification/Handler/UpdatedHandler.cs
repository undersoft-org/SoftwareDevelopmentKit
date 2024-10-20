using MediatR;

namespace Undersoft.SDK.Service.Operation.Command.Notification.Handler;

using Undersoft.SDK.Proxies;
using Undersoft.SDK.Service.Data.Event;
using Undersoft.SDK.Service.Data.Store;
using Undersoft.SDK.Service.Data.Store.Repository;
using Undersoft.SDK.Service.Operation.Command.Notification;

public class UpdatedHandler<TStore, TEntity, TDto>
    : INotificationHandler<Updated<TStore, TEntity, TDto>>
    where TDto : class, IOrigin, IInnerProxy
    where TEntity : class, IOrigin, IInnerProxy
    where TStore : IDataServerStore
{
    protected readonly IStoreRepository<Event> _eventStore;
    protected readonly IStoreRepository<TEntity> _repository;

    public UpdatedHandler() { }

    public UpdatedHandler(IStoreRepository<IEventStore, Event> eventStore)
    {
        _eventStore = eventStore;
    }

    public UpdatedHandler(
        IStoreRepository<IReportStore, TEntity> repository,
        IStoreRepository<IEventStore, Event> eventStore
    )
    {
        _repository = repository;
        _eventStore = eventStore;
    }

    public virtual async Task Handle(
        Updated<TStore, TEntity, TDto> request,
        CancellationToken cancellationToken
    )
    {
        if (_eventStore != null)
            _eventStore.Add(request);

        if (_repository == null || request.Command.Mode != PublishMode.Propagate)
            return;

        TEntity result;
        if (request.Predicate == null)
            result = await _repository.SetBy(request.Command.Contract);
        else if (request.Conditions == null)
            result = await _repository.SetBy(request.Command.Contract, request.Predicate);
        else
            result = await _repository.SetBy(
                request.Command.Contract,
                request.Predicate,
                request.Conditions
            );

        if (result == null)
            throw new Exception(
                $"{$"{GetType().Name} for entity "}{$"{typeof(TEntity).Name} unable update report"}"
            );

        request.PublishStatus = PublishStatus.Complete;
    }
}
