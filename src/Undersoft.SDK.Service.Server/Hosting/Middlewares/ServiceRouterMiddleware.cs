using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace Undersoft.SDK.Service.Server.Hosting.Middlewares;

public class ServiceRouterMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServicer _servicer;
    public ServiceRouterMiddleware(RequestDelegate next, IServicer servicer)
    {
        _next = next;
        _servicer = servicer;
    }
    public async Task InvokeAsync(HttpContext context)
    {
        var originalUrl = new Uri(context.Request.GetDisplayUrl());
        var path = originalUrl.AbsolutePath.Substring(1);

        var storeType = _servicer.Configuration.StoreTypes(path);
        if (storeType != null)
        {
            var contextType = OpenDataRegistry.GetLinkedContextType(storeType);
            if (contextType != null)
            {
                var connectionString = _servicer.Configuration.ClientConnectionString(contextType.FullName);
                if (connectionString != null)
                {
                    context.Response.Redirect($"{connectionString}{path}");
                }
            }

            await _next(context);
        }
    }
}