﻿using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Undersoft.SDK.Service.Access;
using Undersoft.SDK.Service.Behaviour;
using Undersoft.SDK.Service.Data.Client;
using Undersoft.SDK.Service.Data.Contract;
using Undersoft.SDK.Service.Data.Entity;
using Undersoft.SDK.Service.Data.Repository;
using Undersoft.SDK.Service.Data.Repository.Client;
using Undersoft.SDK.Service.Data.Repository.Source;
using Undersoft.SDK.Utilities;

namespace Undersoft.SDK.Service;

using Configuration;
using Data.Cache;

public partial class ServiceSetup : IServiceSetup
{
    protected string[] apiVersions = new string[1] { "v1" };
    protected Assembly[] Assemblies;

    protected IServiceConfiguration configuration => manager.Configuration;
    protected IServiceManager manager { get; }
    protected IServiceRegistry registry => manager.Registry;
    protected IServiceCollection services => registry.Services;    

    public ServiceSetup(IServiceCollection services) : this(services, null)
    {        
    }

    public ServiceSetup(IServiceCollection services, IConfiguration configuration)
    {
        manager = new ServiceManager(services, configuration);
        AddStoreRoutes();
        registry.MergeServices(true);
    }

    public IServiceRegistry Services => registry;

    public IServiceManager Manager => manager;

    protected virtual Func<Type> ForMainBehaviour { get; set; } 
        = () => typeof(LoggingBehaviour<,>);

    public IServiceSetup AddCaching()
    {
        registry.AddObject(RootCache.Catalog);

        Type[] stores = new Type[]
        {
            typeof(IEntryStore),
            typeof(IReportStore),
            typeof(IEventStore),
            typeof(IDataStore),
            typeof(IAccountStore)
        };
        foreach (Type item in stores)
        {
            AddStoreCache(item);
        }

        return this;
    }

    public IServiceSetup AddStoreRoutes()
    {
        var storeRoutesOptions = new StoreRouteRegistry(configuration.StoreRoutes());        
        registry.AddObject(storeRoutesOptions);
        return this;
    }

    public virtual IServiceSetup AddSourceProviderConfiguration()
    {
        ServiceManager.AddRootObject<ISourceProviderConfiguration>(
            new ServiceSourceProviderConfiguration(registry)
        );

        return this;
    }

    public void AddJsonSerializerDefaults()
    {
        var flds = typeof(JsonSerializerOptions).GetRuntimeFields();
        flds.Single(f => f.Name == "_defaultIgnoreCondition")
            .SetValue(JsonSerializerOptions.Default, JsonIgnoreCondition.WhenWritingNull);
        flds.Single(f => f.Name == "_referenceHandler")
            .SetValue(JsonSerializerOptions.Default, ReferenceHandler.IgnoreCycles);
    }

    public IServiceSetup AddLogging()
    {
        registry.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));
        return this;
    }

    public IServiceSetup AddPropertyInjection()
    {
        manager.AddPropertyInjection();

        return this;
    }

    public IServiceSetup AddImplementations()
    {
        registry.AddScoped<IServicer, Servicer>();
        registry.AddTransient<IInvoker, Invoker>();
        registry.AddScoped<IAuthorization, Authorization>();

        IServiceCollection deck = registry
            .AddTransient<ISeries<IEntity>, Listing<IEntity>>()
            .AddTransient<ISeries<IContract>, Chain<IContract>>()
            .AddTransient<ISeries<IEntity>, Registry<IEntity>>()
            .AddTransient<ISeries<IContract>, Catalog<IContract>>()
            .AddScoped<ITypedSeries<IEntity>, TypedRegistry<IEntity>>()
            .AddScoped<ITypedSeries<IContract>, TypedCatalog<IContract>>();

        registry.MergeServices(true);

        return this;
    }

    public IServiceSetup AddValidators(Assembly[] assemblies = null)
    {
        registry.AddValidatorsFromAssemblies(assemblies ??= [ Assembly.GetExecutingAssembly() ]);

        return this;
    }

    public IServiceSetup AddMediator(Assembly[] assemblies = null)
    {
        registry.AddTransient(typeof(IPipelineBehavior<,>), ForMainBehaviour());

        registry.AddTransient(typeof(IPipelineBehavior<,>), typeof(CommandValidationBehaviour<,>));
        registry.AddTransient(typeof(IPipelineBehavior<,>), typeof(CommandSetValidationBehaviour<,>));
        registry.AddTransient(typeof(IPipelineBehavior<,>), typeof(QueryValidationBehaviour<,>));
        registry.AddTransient(typeof(IPipelineBehavior<,>), typeof(RemoteQueryValidationBehaviour<,>)
        );
        registry.AddTransient(
            typeof(IPipelineBehavior<,>),
            typeof(RemoteCommandSetValidationBehaviour<,>)
        );
        registry.AddTransient(
            typeof(IPipelineBehavior<,>),
            typeof(RemoteCommandValidationBehaviour<,>)
        );

        registry.AddMediatR(assemblies ??= [ Assembly.GetExecutingAssembly() ]);

        return this;
    }

    public IServiceSetup AddRepositoryClients()
    {        
        return AddRepositoryClients(configuration.Clients().ForEach(c => AssemblyUtilities.FindType(c.Key)).Commit());
    }

    public IServiceSetup AddRepositoryClients(Type[] serviceTypes)
    {
        IServiceConfiguration config = configuration;

        IEnumerable<IConfigurationSection> clients = config.Clients();
        RepositoryClients repoClients = new RepositoryClients();

        registry.AddSingleton(registry.AddObject<IRepositoryClients>(repoClients).Value);

        foreach (IConfigurationSection client in clients)
        {
            ClientProvider provider = config.ClientProvider(client);
            string connectionString = config.ClientConnectionString(client).Trim();
            int poolsize = config.ClientPoolSize(client);
            
            Type contextType = serviceTypes.Where(t => t.FullName.Contains(client.Key)).FirstOrDefault();

            if (
                (provider == ClientProvider.None)
                || (connectionString == null)
                || (contextType == null)
            )
                continue;

            if (!connectionString.EndsWith('/'))
                connectionString += "/";

            string routePrefix = AddDataClientPrefix(contextType).Trim();
            if (routePrefix.StartsWith('/'))
                routePrefix = routePrefix.Substring(1);

            string _connectionString = $"{connectionString}{routePrefix}";

            Type iRepoType = typeof(IRepositoryClient<>).MakeGenericType(contextType);
            Type repoType = typeof(RepositoryClient<>).MakeGenericType(contextType);

            IRepositoryClient repoClient = (IRepositoryClient)repoType.New(_connectionString);

            Type storeType = DataClientRegistry.GetLinkedStoreType(contextType);
            Type storeDbType = typeof(DataClient<>).MakeGenericType(storeType);
            Type storeRepoType = typeof(RepositoryClient<>).MakeGenericType(storeDbType);

            IRepositoryClient storeClient = (IRepositoryClient)storeRepoType.New(repoClient);

            Type istoreRepoType = typeof(IRepositoryClient<>).MakeGenericType(storeDbType);
            Type ipoolRepoType = typeof(IRepositoryContextPool<>).MakeGenericType(storeDbType);
            Type ifactoryRepoType = typeof(IRepositoryContextFactory<>).MakeGenericType(
                storeDbType
            );
            Type idataRepoType = typeof(IRepositoryContext<>).MakeGenericType(storeDbType);

            repoClient.PoolSize = poolsize;

            repoClients.Add(repoClient);

            registry.AddObject(iRepoType, repoClient);
            registry.AddObject(repoType, repoClient);

            registry.AddObject(istoreRepoType, storeClient);
            registry.AddObject(ipoolRepoType, storeClient);
            registry.AddObject(ifactoryRepoType, storeClient);
            registry.AddObject(idataRepoType, storeClient);
            registry.AddObject(storeRepoType, storeClient);

            repoClient.PoolSize = poolsize;
            repoClient.CreatePool();

            AddStoreCache(storeType);
        }

        return this;
    }

    public virtual IServiceSetup ConfigureServices(
        Type[] clientTypes = null,
        Action<IServiceSetup> services = null
    )
    {
        AddLogging();

        AddJsonSerializerDefaults();

        if (clientTypes != null)
            AddRepositoryClients(clientTypes);
        else
            AddRepositoryClients();

        AddCaching();

        if (services != null)
            services(this);

        AddImplementations();

        return this;
    }

    public IServiceSetup MergeServices()
    {
        registry.MergeServices();

        return this;
    }

    public IServiceSetup AddStoreCache(Type tstore)
    {
        Type idatacache = typeof(IStoreCache<>).MakeGenericType(tstore);
        Type datacache = typeof(StoreCache<>).MakeGenericType(tstore);
        
        bool haveIfaceCache = registry.ContainsService(idatacache);
        bool haveCache = registry.ContainsService(datacache);

        if (haveIfaceCache && haveCache)
            return this;

        object cache = datacache.New(registry.GetObject<IDataCache>());

        if (!haveIfaceCache)
            registry.AddObject(idatacache, cache);

        if (!haveCache)
            registry.AddObject(datacache, cache);

        return this;
    }

    public IServiceSetup AddStoreCache<TStore>()
    {
        return AddStoreCache(typeof(TStore));
    }

    private string AddDataClientPrefix(Type contextType, string routePrefix = null)
    {
        Type iface = DataClientRegistry.GetLinkedStoreType(contextType);
        return GetStoreRoute(iface, routePrefix);
    }

    protected string GetStoreRoute(Type iface, string routePrefix = null)
    {
        var sro = registry.GetObject<StoreRouteRegistry>();

        if (sro != null)
        {
            if(sro.TryGet(iface, out (Type, string) route))            
                return route.Item2;
        }

        switch (iface)
        {
            case IEntryStore:
                return StoreRoutes.EntryStore;
            case IReportStore:
                return StoreRoutes.ReportStore;
            case IEventStore:
                return StoreRoutes.EventStore;
            case IAccountStore:
                return StoreRoutes.AccountStore;
            default:
                return StoreRoutes.DataStore;
        };        
    }
}
