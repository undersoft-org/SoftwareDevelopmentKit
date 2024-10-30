using MediatR;

namespace Undersoft.SDK.Service.Infrastructure.Telemetry;

using Logging;

public class TelemetryBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, IOperation
    where TResponse : IOperation
{
    private OperationTelemetry _telemetry;

    public TelemetryBehaviour(OperationTelemetry telemetry)
    {
        _telemetry = telemetry;        
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        using var activity = _telemetry.StartActivity(request);
       
        var response = await next();
       
        _telemetry.AddTags(activity, request, response);

        return response;
    }  
}
