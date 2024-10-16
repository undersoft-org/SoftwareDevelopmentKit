﻿using MediatR;

namespace Undersoft.SDK.Service.Operation.Command.Notification.Handler;
using Series;
using Undersoft.SDK.Proxies;
using Undersoft.SDK.Service.Data.Event;
using Undersoft.SDK.Service.Data.Store;
using Undersoft.SDK.Service.Data.Store.Repository;
using Undersoft.SDK.Service.Operation.Command.Notification;

public class ChangedDocumentSetHandler<TStore, TEntity, TDto>
    : INotificationHandler<ChangedDocumentSet<TStore, TEntity, TDto>>
    where TDto : class, IOrigin, IInnerProxy
    where TEntity : class, IOrigin, IInnerProxy
    where TStore : IDataServerStore
{
    protected readonly IStoreRepository<TEntity> _repository;
    protected readonly IStoreRepository<Event> _eventStore;

    public ChangedDocumentSetHandler() { }

    public ChangedDocumentSetHandler(IStoreRepository<IEventStore, Event> eventStore)
    {
        _eventStore = eventStore;
    }

    public ChangedDocumentSetHandler(
        IStoreRepository<IReportStore, TEntity> repository,
        IStoreRepository<IEventStore, Event> eventStore
    )
    {
        _repository = repository;
        _eventStore = eventStore;
    }

    public virtual Task Handle(
        ChangedDocumentSet<TStore, TEntity, TDto> request,
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

        if (_repository == null || request.PublishMode != EventPublishMode.PropagateCommand)
            return Task.CompletedTask;

        ISeries<TEntity> entities;
        if (request.Predicate == null)
            entities = _repository
                .PatchDocument(request.Select(d => d.Command.Contract))
                .ToListing();
        else
            entities = _repository
                .PatchDocument(
                    request.Select(d => d.Command.Contract),
                    request.Predicate
                )
                .ToListing();

        request.ForEach(
            (r) =>
            {
                _ = entities.ContainsKey(r.EntityId)
                    ? r.PublishStatus = EventPublishStatus.Complete
                    : r.PublishStatus = EventPublishStatus.Uncomplete;
            }
        );
        return Task.CompletedTask;
    }
}
