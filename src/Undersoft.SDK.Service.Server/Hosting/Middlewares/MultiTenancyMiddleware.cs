using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Undersoft.SDK.Service.Access.MultiTenancy;
using Undersoft.SDK.Service.Server.Extensions;

namespace Undersoft.SDK.Service.Server.Hosting.Middlewares;

public class MultiTenancyMiddleware
{
    private readonly RequestDelegate _next;
    private IServicer _servicer;

    public MultiTenancyMiddleware(RequestDelegate next, IServicer servicer)
    {
        _next = next;
        _servicer = servicer;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestServicer = GetRequestServicer(context);

        if (
            context.User.Identity.IsAuthenticated
            && long.TryParse(
                context.User.Claims.FirstOrDefault(c => c.Type == "tenant_id")?.Value,
                out var tenantId
            )
        )
        {
            if (_servicer.GetKeyedObject<ITenant>(tenantId) == null)
            {
                await ApplySourceMigrations(
                        new ServerSetup(new Tenant() { Id = tenantId })
                            .ConfigureTenant(_servicer)
                            .Manager
                    )
                    .ConfigureAwait(false);
            }

            context.SetTenantServicer(requestServicer, tenantId);
        }
        else
            requestServicer.SetServicer();

        await _next(context);
    }

    private IServicer GetRequestServicer(HttpContext context)
    {
        KeyValuePair<Type, object>? requestServiceProvider = context.Features.FirstOrDefault(kvp =>
            kvp.Key == typeof(IServiceProvidersFeature)
        );
        
        if (requestServiceProvider != null)
            return (
                (IServiceProvidersFeature)requestServiceProvider.Value.Value
            ).RequestServices.GetService<IServicer>();
        
        context.Error<Weblog, Exception>(
            "Unable to get request service provider from http context"
        );
        return null;
    }

    private async Task ApplySourceMigrations(IServiceManager manager)
    {
        using (IServiceScope scope = manager.CreateScope())
        {
            try
            {
                await Task.WhenAll(
                        scope
                            .ServiceProvider.GetRequiredService<IServicer>()
                            .GetSources()
                            .ForEach(async e =>
                                await e.Context.Database.MigrateAsync().ConfigureAwait(false)
                            )
                    )
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.Error<Applog>(
                    $"Model migration to data sources failed: {ex.Message}",
                    null,
                    ex
                );
            }
        }
    }
}
