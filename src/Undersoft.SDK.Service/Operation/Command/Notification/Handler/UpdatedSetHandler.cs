﻿using MediatR;

namespace Undersoft.SDK.Service.Operation.Command.Notification.Handler;
using Series;
using Undersoft.SDK.Proxies;
using Undersoft.SDK.Service.Data.Event;
using Undersoft.SDK.Service.Data.Store;
using Undersoft.SDK.Service.Data.Store.Repository;
using Undersoft.SDK.Service.Operation.Command.Notification;

public class UpdatedSetHandler<TStore, TEntity, TDto>
    : INotificationHandler<UpdatedSet<TStore, TEntity, TDto>>
    where TDto : class, IOrigin, IInnerProxy
    where TEntity : class, IOrigin, IInnerProxy
    where TStore : IDataServerStore
{
    protected readonly IStoreRepository<Event> _eventStore;
    protected readonly IStoreRepository<TEntity> _repository;

    public UpdatedSetHandler() { }

    public UpdatedSetHandler(IStoreRepository<IEventStore, Event> eventStore)
    {
        _eventStore = eventStore;
    }

    public UpdatedSetHandler(
        IStoreRepository<IReportStore, TEntity> repository,
        IStoreRepository<IEventStore, Event> eventStore
    )
    {
        _repository = repository;
        _eventStore = eventStore;
    }

    public virtual Task Handle(
        UpdatedSet<TStore, TEntity, TDto> request,
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
        if (request.Predicate == null)
            entities = _repository
                .SetBy(request.Select(d => d.Command.Contract))
                .ToCatalog();
        else if (request.Conditions == null)
            entities = _repository
                .SetBy(request.Select(d => d.Command.Contract), request.Predicate)
                .ToCatalog();
        else
            entities = _repository
                .SetBy(
                    request.Select(d => d.Command.Contract),
                    request.Predicate,
                    request.Conditions
                )
                .ToCatalog();

        request.ForEach(
            (r) =>
            {
                _ = entities.ContainsKey(r.EntityId)
                    ? (r.PublishStatus = PublishStatus.Complete)
                    : (r.PublishStatus = PublishStatus.Uncomplete);
            }
        );
        return Task.CompletedTask;
    }
}
