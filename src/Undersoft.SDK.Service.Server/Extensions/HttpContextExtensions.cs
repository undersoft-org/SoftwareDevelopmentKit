using Microsoft.AspNetCore.Http;
using Undersoft.SDK.Service.Access;

namespace Undersoft.SDK.Service.Server.Extensions;

public static class HttpContextExtensions
{
    public static IServicer SetTenantServicer(this IHttpContextAccessor accessor, IServicer servicer, long tenantId = 0)
    {
        return SetTenantServicer(accessor.HttpContext, servicer, tenantId);
    }

    public static IServicer SetTenantServicer(this HttpContext context, IServicer servicer, long tenantId = 0)
    {
        if (tenantId == 0)
            return SetAuthorization(context, servicer.SetTenantServicer(context.User, servicer));
        return SetAuthorization(context, servicer.SetTenantServicer(tenantId, servicer));
    }

    private static IServicer SetAuthorization(this HttpContext context, IServicer servicer)
    {
        var token = context.Request.Headers[nameof(Authorization)].FirstOrDefault();

        if (token != null)
            servicer.GetService<IServicer>().SetAuthorization(
                new Authorization()
                {
                    Credentials = new Credentials()
                    {
                        SessionToken = token.Split(" ").LastOrDefault(),
                    },
                }
            );

        return servicer;
    }
}
