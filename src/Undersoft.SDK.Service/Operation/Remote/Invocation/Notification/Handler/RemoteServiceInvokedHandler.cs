using FluentValidation.Results;
using MediatR;
using Undersoft.SDK.Service.Data.Event;
using Undersoft.SDK.Service.Data.Store.Repository;

namespace Undersoft.SDK.Service.Operation.Remote.Invocation.Notification.Handler;

public class RemoteServiceInvokedHandler<TStore, TService, TModel>
    : INotificationHandler<RemoteServiceInvoked<TStore, TService, TModel>>
    where TService : class
    where TModel : class, IOrigin
    where TStore : IDataServiceStore
{
    protected readonly IStoreRepository<Event> _eventStore;

    public RemoteServiceInvokedHandler() { }

    public RemoteServiceInvokedHandler(
        IStoreRepository<IEventStore, Event> eventStore
    )
    {
        _eventStore = eventStore;
    }

    public virtual Task Handle(
        RemoteServiceInvoked<TStore, TService, TModel> request,
        CancellationToken cancellationToken
    )
    {
        return Task.Run(
            () =>
            {
                try
                {
                    if (_eventStore.Add(request) == null)
                        throw new Exception(
                            $"{$"{GetType().Name} "}{$"for contract {typeof(TModel).Name} unable add event"}"
                        );
                }
                catch (Exception ex)
                {
                    request.Command.Validation.Errors.Add(
                        new ValidationFailure(string.Empty, ex.Message)
                    );
                    this.Failure<Domainlog>(ex.Message, request.Command.ErrorMessages, ex);
                    request.PublishStatus = PublishStatus.Error;
                }
            },
            cancellationToken
        );
    }
}
