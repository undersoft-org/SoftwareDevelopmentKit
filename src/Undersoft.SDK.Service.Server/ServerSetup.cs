using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Exporter;
using Undersoft.SDK.Service.Server.Accounts.Identity;

namespace Undersoft.SDK.Service.Server;

using Accounts;
using Accounts.Email;
using Documentation;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics.Metrics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Undersoft.SDK.Service.Access;
using Undersoft.SDK.Service.Access.MultiTenancy;
using Undersoft.SDK.Service.Configuration;
using Undersoft.SDK.Service.Data.Repository.Source;
using Undersoft.SDK.Service.Data.Store;
using Undersoft.SDK.Service.Hosting;
using Undersoft.SDK.Service.Infrastructure.Telemetry;
using Undersoft.SDK.Service.Server.Accounts.Identity;
using Undersoft.SDK.Service.Server.Accounts.Tokens;
using Undersoft.SDK.Service.Server.Builders;
using Undersoft.SDK.Utilities;
using Role = Role;

public partial class ServerSetup : ServiceSetup, IServerSetup
{
    protected IMvcBuilder _mvc;
    protected ITenant _tenant;

    public ServerSetup(ITenant tenant)
        : base(new ServiceCollection())
    {
        _tenant = tenant;
    }

    public ServerSetup(IServiceCollection services, IMvcBuilder mvcBuilder = null)
        : base(services)
    {
        if (mvcBuilder != null)
            _mvc = mvcBuilder;
        else
            _mvc = services.AddControllers();

        registry.MergeServices(services);
    }

    public ServerSetup(IServiceCollection services, IConfiguration configuration)
        : base(services, configuration)
    {
        _mvc = services.AddControllers();
        registry.MergeServices(services);
    }

    public IServerSetup AddDataServer<TServiceStore>(
        DataServiceTypes dataServiceTypes = DataServiceTypes.All,
        Action<DataServerBuilder> builder = null
    )
        where TServiceStore : IDataStore
    {       
        if ((dataServiceTypes & DataServiceTypes.Open) > 0)
        {
            var ds = new DataServiceBuilder<TServiceStore>(registry);
            if (builder != null)
                builder.Invoke(ds);
            ds.Build();
            ds.AddDataServicer(_mvc);
        }
        if ((dataServiceTypes & DataServiceTypes.Stream) > 0)
        {
            var ds = new StreamServiceBuilder<TServiceStore>(registry);
            if (builder != null)
                builder.Invoke(ds);
            ds.Build();
            ds.AddStreamServicer();
        }
        
        registry.MergeServices(true);
        return this;
    }

    public IServiceSetup ConfigureTenant(IServicer mainServicer)
    {
        var mainManager = mainServicer.GetManager();
        mainManager.AddKeyedObject(_tenant.Id, _tenant);
        mainManager.AddKeyedObject(_tenant.Id, manager);
        manager.AddKeyedObject(_tenant.Id, _tenant);

        AddSourceProviderConfiguration();
        AddRepositorySources();
        AddDataStoreImplementations();

        AddOpenTelemetry();

        base.ConfigureServices(null);

        AddValidators(null);
        AddMediator(null);

        registry.Configure<DataProtectionTokenProviderOptions>(o =>
            o.TokenLifespan = TimeSpan.FromMinutes(30)
        );
        registry.AddTransient<IAccountManager, AccountManager>();

        AddServerSetupCqrsImplementations();
        AddServerSetupInvocationImplementations();
        AddServerSetupRemoteCqrsImplementations();
        AddServerSetupRemoteInvocationImplementations();
        Services.AddOpenDataRemoteImplementations();

        Services.MergeServices(true);
        manager.BuildInternalProvider();

        mainManager.Registry.MergeServices(true);
        mainManager.BuildInternalProvider();

        return this;
    }

    public override IServiceSetup AddSourceProviderConfiguration()
    {
        var sspc = new ServerSourceProviderConfiguration(manager.Registry);
        registry.AddObject<ISourceProviderConfiguration>(sspc);
        ServiceManager.AddRootObject<ISourceProviderConfiguration>(sspc);

        return this;
    }

    public IServiceSetup AddJsonOptions()
    {
        _mvc.AddJsonOptions(json =>
        {
            var options = json.JsonSerializerOptions;
            options.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.NumberHandling = JsonNumberHandling.AllowReadingFromString;
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.IgnoreReadOnlyProperties = true;
            options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, true));
        });
        return this;
    }

    public IServiceSetup AddHealthChecks()
    {
        services.AddHealthChecks();
        return this;
    }

    public IServiceSetup AddOpenTelemetry()
    {
        var config = configuration;

        if (!config.GetSection("OpenTelemetry").GetChildren().Any() 
            || config.GetValue<string>("UseTracingExporter") == null 
            || config.GetValue<string>("UseMetricsExporter") == null)
            return this;

        var tracingExporter = config["UseTracingExporter"].ToLowerInvariant();
        var metricsExporter = config["UseMetricsExporter"].ToLowerInvariant();

        Action<ResourceBuilder> configureResource = r =>
            r.AddService(
                serviceName: config.Name,
                serviceVersion: Environment.Version.ToString(),
                serviceInstanceId: Environment.MachineName
            );

        var histogramAggregation = config.GetValue<string>("HistogramAggregation");
        if(histogramAggregation != null)
            histogramAggregation = histogramAggregation.ToLowerInvariant();

        var _operationInstrumentation = new OperationTelemetry();        
        
        registry.AddObject(typeof(OperationTelemetry), _operationInstrumentation);

        ForMainBehaviour = () => typeof(TelemetryBehaviour<,>);

        var otel = registry.AddOpenTelemetry();
        otel.ConfigureResource(configureResource);
        otel.WithTracing(builder =>
            {
                builder.AddAspNetCoreInstrumentation();
                builder.AddHttpClientInstrumentation();
                builder.AddSource(_operationInstrumentation.ActivitySource.Name);

                switch (tracingExporter)
                {
                    case "jaeger":
                        builder.AddJaegerExporter();

                        builder.ConfigureServices(services =>
                        {
                            services.Configure<JaegerExporterOptions>(config.GetSection("Jaeger"));

                            services.AddHttpClient(
                                "JaegerExporter",
                                configureClient: (client) =>
                                    client.DefaultRequestHeaders.Add(
                                        "X-Title",
                                        config.Name
                                            + " ,OS="
                                            + Environment.OSVersion
                                            + ",ServiceName="
                                            + Environment.MachineName
                                            + ",Domain="
                                            + Environment.UserDomainName
                                    )
                            );
                        });
                        break;

                    case "zipkin":
                        builder.AddZipkinExporter();

                        builder.ConfigureServices(services =>
                        {
                            services.Configure<ZipkinExporterOptions>(config.GetSection("Zipkin"));
                        });
                        break;

                    case "otlp":
                        builder.AddOtlpExporter(otlpOptions =>
                        {
                            otlpOptions.Endpoint = new Uri(config.GetValue<string>("Otlp:Source"));
                        });
                        break;

                    default:
                        builder.AddConsoleExporter();
                        break;
                }
            });
            otel.WithMetrics(builder =>
            {
                // Metrics

                // Ensure the MeterProvider subscribes to any custom Meters.
                //builder.AddRuntimeInstrumentation();                
                //builder.AddHttpClientInstrumentation();
                builder.AddAspNetCoreInstrumentation();
                builder.AddMeter(_operationInstrumentation.Meter.Name);
                builder.AddMeter("Microsoft.AspNetCore.Hosting");
                builder.AddMeter("Microsoft.AspNetCore.Server.Kestrel");

                switch (histogramAggregation)
                {
                    case "explicit":
                        builder.AddView(instrument =>
                        {
                            return
                                instrument.GetType().GetGenericTypeDefinition()
                                == typeof(Histogram<>)
                                ? new ExplicitBucketHistogramConfiguration()
                                : null;
                        });
                        break;
                    default:
                        break;
                }

                switch (metricsExporter)
                {
                    case "prometheus":
                        builder.AddPrometheusExporter();
                        break;
                    case "otlp":
                        builder.AddOtlpExporter(otlpOptions =>
                        {
                            otlpOptions.Endpoint = new Uri(config.GetValue<string>("Otlp:Host"));
                        });
                        break;
                    default:
                        builder.AddConsoleExporter();
                        break;
                }
            });

        Services.MergeServices(true);

        return this;
    }

    public IServerSetup AddAccessServer<TContext, TAccount>()
        where TContext : DbContext
        where TAccount : class, IOrigin, IAuthorization
    {
        registry
            .Services.AddIdentity<AccountUser, Role>(options =>
            {
                options.SignIn.RequireConfirmedAccount = true;
                options.Tokens.ProviderMap.Add(
                    "AccountEmailConfirmationTokenProvider",
                    new TokenProviderDescriptor(
                        typeof(AccountEmailConfirmationTokenProvider<AccountUser>)
                    )
                );
                options.Tokens.EmailConfirmationTokenProvider =
                    "AccountEmailConfirmationTokenProvider";
                options.Tokens.ProviderMap.Add(
                    "AccountPasswordResetTokenProvider",
                    new TokenProviderDescriptor(
                        typeof(AccountPasswordResetTokenProvider<AccountUser>)
                    )
                );
                options.Tokens.PasswordResetTokenProvider = "AccountPasswordResetTokenProvider";
                options.Tokens.ProviderMap.Add(
                    "AccountChangeEmailTokenProvider",
                    new TokenProviderDescriptor(
                        typeof(AccountChangeEmailTokenProvider<AccountUser>)
                    )
                );
                options.Tokens.ChangeEmailTokenProvider = "AccountChangeEmailTokenProvider";
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<TContext>();

        registry.AddTransient<AccountEmailConfirmationTokenProvider<AccountUser>>();
        registry.AddTransient<AccountPasswordResetTokenProvider<AccountUser>>();
        registry.AddTransient<AccountChangeEmailTokenProvider<AccountUser>>();        

        registry.AddTransient<IAccountManager, AccountManager>();
        registry.AddTransient<AccountService<TAccount>>();
        registry.AddTransient<IEmailSender, AccountEmailSender>();
        registry.Configure<AccountEmailSenderOptions>(configuration);

        return this;
    }

    public IServerSetup AddAuthentication()
    {
        var jwtOptions = new AccountTokenOptions();
        var jwtFactory = new AccountTokenGenerator(30, jwtOptions);
        registry.Configure<DataProtectionTokenProviderOptions>(o =>
            o.TokenLifespan = TimeSpan.FromMinutes(30)
        );

        registry.AddObject(jwtFactory);

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    RoleClaimType = "role",
                    NameClaimType = "name",
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(jwtOptions.SecurityKey),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true
                };
            });
        return this;
    }

    public IServerSetup AddAuthorization()
    {
        var ao = configuration.AccessOptions;

        services.AddAuthorization(options =>
        {
            options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
            
            if(ao.Scopes != null)
                ao.Scopes.ForEach(s => options.AddPolicy(s, policy => policy.RequireClaim("SessionScope", s)));
            if (ao.Roles != null)
                ao.Roles.ForEach(s => options.AddPolicy(s, policy => policy.RequireRole(s)));
            if (ao.Claims != null)
                ao.Claims.ForEach(s => options.AddPolicy(s, policy => policy.RequireClaim(s)));
        });

        return this;
    }

    public IServerSetup AddSwagger()
    {
        string ver = configuration.Version;
        var ao = configuration.Name;

        registry.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(
                configuration.Version,
                new OpenApiInfo { Title = configuration.Name, Version = configuration.Version }
            );

            options.OperationFilter<JsonIgnoreFilter>();
            options.DocumentFilter<IgnoreApiDocument>();

            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "JWT Authentication",
                Description = "Enter your JWT token in this field",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            };

            options.AddSecurityDefinition("Bearer", securityScheme);

            var securityRequirement = new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    new string[] { }
                }
            };

            options.AddSecurityRequirement(securityRequirement);
        });

        return this;
    }

    public IServiceSetup AddRepositorySources()
    {
        return AddRepositorySources(configuration
            .Sources()
            .ForEach(c => AssemblyUtilities.FindType(c.Key))
            .Commit());
    }

    public IServiceSetup AddRepositorySources(Type[] storeTypes)
    {
        IServiceConfiguration config = configuration;
        IEnumerable<IConfigurationSection> sources = config.Sources();

        RepositorySources repoSources = new RepositorySources();
        registry.AddSingleton(registry.AddObject<IRepositorySources>(repoSources).Value);

        var providerNotExists = new HashSet<string>();

        foreach (IConfigurationSection source in sources)
        {
            string connectionString = config.SourceConnectionString(source);
            
            if (_tenant != null && connectionString.Contains("db"))
                connectionString = connectionString.Replace(
                    "db",
                    $"{_tenant.Id.ToString()}-db"
                );

            SourceProvider provider = config.SourceProvider(source);
            int poolsize = config.SourcePoolSize(source);
            Type contextType = storeTypes.Where(t => t.FullName == source.Key).FirstOrDefault();

            if (
                (provider == SourceProvider.None)
                || (connectionString == null)
                || (contextType == null)
            )
            {
                continue;
            }

            if (providerNotExists.Add(provider.ToString()))
            {
                registry.RegisterEntityFrameworkSourceProvider(provider);
                registry.MergeServices(true);
            }

            Type iRepoType = typeof(IRepositorySource<>).MakeGenericType(contextType);
            Type repoType = typeof(RepositorySource<>).MakeGenericType(contextType);
            Type repoOptionsType = typeof(DbContextOptions<>).MakeGenericType(contextType);
            Type repoOptionsBuilderType = typeof(DbContextOptionsBuilder<>).MakeGenericType(contextType);

            var builder = registry.GetObject<ISourceProviderConfiguration>();
            var options = builder.BuildOptions(repoOptionsBuilderType.New<DbContextOptionsBuilder>(), provider, connectionString).Options;

            IRepositorySource repoSource = (IRepositorySource)repoType.New(options);

            Type storeDbType = typeof(DataStoreContext<>).MakeGenericType(DataStoreRegistry.GetStoreType(contextType));
            Type storeOptionsType = typeof(DbContextOptions<>).MakeGenericType(storeDbType);
            Type storeRepoType = typeof(RepositorySource<>).MakeGenericType(storeDbType);

            IRepositorySource storeSource = (IRepositorySource)storeRepoType.New(repoSource);

            Type istoreRepoType = typeof(IRepositorySource<>).MakeGenericType(storeDbType);
            Type ipoolRepoType = typeof(IRepositoryContextPool<>).MakeGenericType(storeDbType);
            Type ifactoryRepoType = typeof(IRepositoryContextFactory<>).MakeGenericType(storeDbType);
            Type idataRepoType = typeof(IRepositoryContext<>).MakeGenericType(storeDbType);

            repoSources.Add(repoSource);

            AddDatabaseConfiguration(repoSource.Context);

            registry.AddObject(contextType, repoSource.Context);

            registry.AddObject(iRepoType, repoSource);
            registry.AddObject(repoType, repoSource);
            registry.AddObject(repoOptionsType, repoSource.Options);
            registry.AddObject(istoreRepoType, storeSource);
            registry.AddObject(ipoolRepoType, storeSource);
            registry.AddObject(ifactoryRepoType, storeSource);
            registry.AddObject(idataRepoType, storeSource);
            registry.AddObject(storeRepoType, storeSource);
            registry.AddObject(storeOptionsType, storeSource.Options);

            repoSource.PoolSize = poolsize;
            repoSource.CreatePool();
        }

        return this;
    }

    private void AddDatabaseConfiguration(IDataStoreContext context)
    {
        DbContext _context = context as DbContext;
        _context.ChangeTracker.AutoDetectChangesEnabled = true;
        _context.ChangeTracker.LazyLoadingEnabled = false;
        _context.Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;
    }

    private string AddDataServiceStorePrefix(Type contextType, string routePrefix = null)
    {
        Type iface = DataStoreRegistry.GetStoreType(contextType);
        return GetStoreRoute(iface, routePrefix);
    }

    public IServerSetup AddApiVersions(string[] apiVersions)
    {
        this.apiVersions = apiVersions;
        return this;
    }

    public IServerSetup ConfigureServer(
        bool includeSwagger = false,
        Type[] sourceTypes = null,
        Type[] clientTypes = null
    )
    {
        Assemblies = AppDomain.CurrentDomain.GetAssemblies();

        AddJsonOptions();
        AddSourceProviderConfiguration();

        if (sourceTypes != null)
            AddRepositorySources(sourceTypes);
        else
            AddRepositorySources();

        AddDataStoreImplementations();

        AddOpenTelemetry();

        base.ConfigureServices(clientTypes);

        AddValidators(Assemblies);
        AddMediator(Assemblies);
        AddServerSetupCqrsImplementations();
        AddServerSetupInvocationImplementations();        
        AddServerSetupRemoteCqrsImplementations();        
        AddServerSetupRemoteInvocationImplementations();

        if (includeSwagger)
            AddSwagger();

        AddAuthentication();
        AddAuthorization();

        Services.MergeServices(true);

        return this;
    }

    public IServerSetup UseServiceClients()
    {
        this.LoadOpenDataEdms().ConfigureAwait(true);
        return this;
    }
}
