﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProtoBuf.Grpc.Server;

namespace Undersoft.SDK.Service.Server.Hosting;

using Logging;
using Microsoft.AspNetCore.Routing;
using Undersoft.SDK.Service.Data.Store;
using Undersoft.SDK.Service.Infrastructure.Telemetry;
using Undersoft.SDK.Service.Server.Builders;
using Undersoft.SDK.Service.Server.Hosting.Middlewares;

public class ServerHostSetup : IServerHostSetup
{
    protected static bool defaultProvider;

    protected readonly IApplicationBuilder _builder;
    protected readonly IWebHostEnvironment _environment;
    protected readonly IServiceManager _manager;

    public ServerHostSetup(IApplicationBuilder application)
    {
        _builder = application;
        _manager = _builder.ApplicationServices.GetService<IServiceManager>();
    }

    public ServerHostSetup(IApplicationBuilder application, IWebHostEnvironment environment)
        : this(application)
    {
        _environment = environment;
    }

    public IServerHostSetup RebuildProviders()
    {
        if (defaultProvider)
        {
            UseDefaultProvider();
        }
        else
        {
            UseInternalProvider();
        }

        return this;
    }

    public IServerHostSetup UseEndpoints(bool useRazorPages = false)
    {
        _builder.UseEndpoints(endpoints =>
        {
            MapStreamControllers(endpoints);         

            _manager.Registry.MergeServices();

            endpoints.MapControllers();

            if (useRazorPages)
            {
                endpoints.MapRazorPages();
                endpoints.MapFallbackToFile("/index.html");
            }

            if (_manager.Registry.ContainsService<OperationTelemetry>())
                endpoints.MapPrometheusScrapingEndpoint();
        });
        
        return this;
    }

    private IEndpointRouteBuilder MapStreamControllers(IEndpointRouteBuilder endpoints)
    {
        var method = typeof(GrpcEndpointRouteBuilderExtensions)
               .GetMethods()
               .Where(m => m.Name.Contains("MapGrpcService"))
               .FirstOrDefault()
               .GetGenericMethodDefinition();

        foreach (var storeControllers in DataServerRegistry.StreamControllers)
        {           
            foreach (var controller in storeControllers)
            {
                method
                    .MakeGenericMethod(controller)
                    .Invoke(endpoints, new object[] { endpoints });
            }
        }

        endpoints.MapCodeFirstGrpcReflectionService();

        return endpoints;
    }

    private IEndpointRouteBuilder MapDataControllers(IEndpointRouteBuilder endpoints)
    {
        foreach (var storeControllers in DataServerRegistry.DataControllers.AsItems())
        {
            string routePrefix = null;
            Type storeType = null;
            var sro = Manager.GetObject<StoreRouteRegistry>();
            if (sro != null)
            {
                if (sro.TryGet(storeControllers.Id, out (Type, string) route))
                {
                    routePrefix = route.Item2;
                    storeType = route.Item1;
                }
            }

            foreach (var _controller in storeControllers.Value)
            {
                var controllerName = _controller.Name.Replace("Controller", "");
                var pattern = string.Format("{0}/{1}", routePrefix, controllerName);
                var patternWithKeyAsSegment = string.Format("{0}/{key}", pattern);
                var patternWithKeyParanthesis = string.Format("{0}({key})", pattern);               
            }
        }

        return endpoints;
    }

    public IServerHostSetup MapFallbackToFile(string filePath)
    {
        _builder.UseEndpoints(endpoints =>
        {
            endpoints.MapFallbackToFile(filePath);
        });

        return this;
    }

    public IServerHostSetup UseServiceClients(int delayInSeconds = 0)
    {
        this.LoadOpenDataEdms(delayInSeconds).ConfigureAwait(false);
        return this;
    }

    public IServerHostSetup UseDataMigrations()
    {
        int tryoutCount = 4;

        using (IServiceScope scope = _manager.CreateScope())
        {

            while (tryoutCount-- > 0)
            {
                try
                {
                    scope.ServiceProvider
                        .GetRequiredService<IServicer>()
                        .GetSources()
                        .ForEach(e => ((IDataStoreContext)e.Context).Database.Migrate());

                   break;
                }
                catch (Exception ex)
                {
                    this.Error<Applog>(
                        "DataServer migration initial create - unable to connect the database engine",
                        null,
                        ex
                    );

                    Task.Delay(5000);
                }
            }
        }

        return this;
    }

    public IServerHostSetup UseDefaultProvider()
    {
        _manager.Registry.MergeServices(true);
        _manager.ReplaceProvider(_builder.ApplicationServices);
        defaultProvider = true;
        return this;
    }

    public IServerHostSetup UseInternalProvider()
    {
        _manager.Registry.MergeServices(true);
        _manager.BuildInternalProvider();
        _builder.ApplicationServices = _manager;
        return this;
    }

    public IServerHostSetup UseServiceServer(bool useMultinancy = true, string[] apiVersions = null)
    {
        UseHeaderForwarding();

        if (_environment.IsDevelopment())
            _builder.UseDeveloperExceptionPage();

        _builder        
            .UseODataQueryRequest()
            .UseODataBatching()
            .UseDefaultFiles()
            #if NET8_0
            .UseStaticFiles()
            #else
            .MapStaticAssets()
            #endif
            .UseRouting()
            .UseCors(o => o.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());

        if (apiVersions != null)
            UseSwaggerSetup(apiVersions);

        _builder.UseAuthentication()
            .UseAuthorization();

        if(useMultinancy)
            UseMultitenancy();

        UseEndpoints();

        return this;
    }

    public IServerHostSetup UseCustomSetup(Action<IServerHostSetup> action)
    {
        action(this);

        return this;
    }

    public IServerHostSetup UseSwaggerSetup(string[] apiVersions)
    {
        if (_builder == null)
        {
            throw new ArgumentNullException(nameof(_builder));
        }

        var ao = _manager.Configuration.AccessOptions;

        _builder
            .UseSwagger()
            .UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint($"/swagger/v1/swagger.json", ao.ServiceName);
                options.InjectStylesheet("/themes/css/theme-feeling-blue.css");
                //s.OAuthClientId(ao.SwaggerClientId);
                //s.OAuthAppName(ao.ApiName);
            });
        return this;
    }

    public IServerHostSetup UseHeaderForwarding()
    {
        var forwardingOptions = new ForwardedHeadersOptions()
        {
            ForwardedHeaders = ForwardedHeaders.All
        };

        forwardingOptions.KnownNetworks.Clear();
        forwardingOptions.KnownProxies.Clear();

        _builder.UseForwardedHeaders(forwardingOptions);

        return this;
    }

    public IServerHostSetup UseJwtMiddleware()
    {
        _builder.UseMiddleware<JwtMiddleware>();
        return this;
    }

    public IServerHostSetup UseMultitenancy()
    {
        _builder.UseMiddleware<MultiTenancyMiddleware>();
        return this;
    }

    public IServerHostSetup UseServiceRouter()
    {
        _builder.UseMiddleware<ServiceRouterMiddleware>();
        return this;
    }

    public IServiceManager Manager => _manager;

    protected IApplicationBuilder Application => _builder;

    protected IWebHostEnvironment LocalEnvironment => _environment;
}
