﻿namespace Undersoft.SDK.Service.Operation.Invocation.Handler;

using MediatR;
using Undersoft.SDK;
using Undersoft.SDK.Service.Data.Store;
using Undersoft.SDK.Service.Operation.Invocation;
using Undersoft.SDK.Service.Operation.Invocation.Notification;

public class ServiceHandler<TStore, TService, TDto>
    : IRequestHandler<Service<TStore, TService, TDto>, Invocation<TDto>>
    where TService : class
    where TDto : class, IOrigin
    where TStore : IDataServerStore
{
    protected readonly IServicer _servicer;

    public ServiceHandler(IServicer servicer)
    {
        _servicer = servicer;
    }

    public async Task<Invocation<TDto>> Handle(
        Service<TStore, TService, TDto> request,
        CancellationToken cancellationToken
    )
    {
        if (!request.Validation.IsValid)
            return request;

        request.Arguments.ResolveArgumentTypes();

        request.Response = await _servicer.Run<TService, TDto>(request.Arguments);

        if (request.Response == null)
            throw new Exception(
                $"{GetType().Name} "
                    + $"for entity {typeof(TDto).Name} "
                    + $"unable create source"
            );

        _ = _servicer.Publish(new ServiceInvoked<TStore, TService, TDto>(request)).ConfigureAwait(false);

        return request;
    }
}
