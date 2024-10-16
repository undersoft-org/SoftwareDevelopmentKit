using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Undersoft.SDK.Service.Access;
using Undersoft.SDK.Service.Access.MultiTenancy;

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

            var token = context.Request.Headers[nameof(Authorization)].FirstOrDefault();
            if (token != null)
                context.Items.Add(
                    nameof(Authorization),
                    new Authorization()
                    {
                        Credentials = new Credentials()
                        {
                            SessionToken = token.Split(" ").LastOrDefault(),
                        },
                    }
                );
        }
        await _next(context);
    }

    private void ReplaceRequestProvider(HttpContext context, long tenantId)
    {
        KeyValuePair<Type, object>? requestServiceProvider = context.Features.FirstOrDefault(kvp =>
            kvp.Key == typeof(IServiceProvidersFeature)
        );
        if (requestServiceProvider != null)
        {
            ((IServiceProvidersFeature)requestServiceProvider.Value.Value).RequestServices =
                _servicer.GetKeyedObject<IServiceManager>(tenantId).CreateScope().ServiceProvider;
        }
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
                    "Unable to establish connection with data source engine",
                    null,
                    ex
                );
            }
        }
    }
}
