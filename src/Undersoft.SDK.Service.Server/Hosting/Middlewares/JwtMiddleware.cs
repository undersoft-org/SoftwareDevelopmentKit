using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Undersoft.SDK.Service.Access;

namespace Undersoft.SDK.Service.Server.Hosting.Middlewares;

public class JwtMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServicer _servicer;

    public JwtMiddleware(RequestDelegate next, IServicer servicer)
    {
        _next = next;
        _servicer = servicer;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.ContainsKey("Authorization"))
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault();
            var session = _servicer.GetServicer(context.User).Session;        
            var auth = session.GetService<IAuthorization>();
            auth.Credentials.SessionToken = token.Split(" ").LastOrDefault();
        }
        await _next(context);
    }
}
