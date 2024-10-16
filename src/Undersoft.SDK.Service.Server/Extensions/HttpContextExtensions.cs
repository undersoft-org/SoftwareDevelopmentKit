using Microsoft.AspNetCore.Http;
using Undersoft.SDK.Service.Access;

namespace Undersoft.SDK.Service.Server.Extensions;

public static class HttpContextExtensions
{
    public static IServicer SetServicer(this IHttpContextAccessor accessor, IServicer servicer)
    {
        servicer.SetTenantServicer(accessor.HttpContext.User, servicer);
        if (accessor.HttpContext.Items.TryGetValue(nameof(Authorization), out var auth))
            servicer.GetService<IServicer>().SetAuthorization(auth as IAuthorization); 
        return servicer;
    }
}
