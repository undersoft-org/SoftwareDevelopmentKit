using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Undersoft.SDK.Service
{
    using Configuration;
    using System.Security.Claims;
    using Undersoft.SDK.Service.Data.Repository;
    using Undersoft.SDK.Service.Hosting;
    using Undersoft.SDK.Utilities;

    public class ServiceManager : RepositoryManager, IServiceManager, IAsyncDisposable
    {
        private new bool disposedValue;

        private static IServiceRegistry rootRegistry;

        private static IServiceConfiguration rootConfiguration;       

        public IServiceProvider RootProvider => GetRootProvider();

        protected virtual IServiceProvider InnerProvider { get; set; }

        public IServiceProvider Provider => GetProvider();

        protected virtual IServiceScope SessionScope { get; set; }

        public IServiceProvider SessionProvider => GetSession();
       
        protected virtual IServiceConfiguration InnerConfiguration { get; set; }

        public IServiceConfiguration Configuration
        {
            get => InnerConfiguration;
            set => InnerConfiguration = value;
        }

        protected virtual IServiceRegistry InnerRegistry { get; set; }

        public IServiceRegistry Registry => InnerRegistry;

        static ServiceManager()
        {
            var sm = new ServiceManager(new ServiceCollection());
            rootRegistry = sm.Registry;
            rootConfiguration = sm.Configuration;
        }

        public ServiceManager() : base()
        {
            Manager = this;
        }

        public ServiceManager(IServiceManager serviceManager) : base()
        {
            SetManager(serviceManager);
        }

        internal ServiceManager(IServiceCollection services) : this()
        {
            if (InnerRegistry == null)
            {
                InnerRegistry = new ServiceRegistry(services, this);
                InnerRegistry.MergeServices(true);
                AddObject<IServiceManager>(this);

                BuildServiceProviderFactory(InnerRegistry);
            }
            else
                InnerRegistry.MergeServices(services, true);

            if (InnerConfiguration == null)
            {
                InnerConfiguration = new ServiceConfiguration(InnerRegistry);
                AddObject(InnerConfiguration);
            }
        }

        public virtual IServiceProviderFactory<IServiceCollection> BuildServiceProviderFactory(IServiceRegistry registry)
        {
            var factory = new ServiceManagerFactory(this);

            AddObject<IServiceRegistry>(registry);
            AddObject<IServiceCollection>(registry);
            AddObject<IServiceProviderFactory<IServiceCollection>>(factory);

            registry.Services.Replace(ServiceDescriptor.Singleton<IServiceProviderFactory<IServiceCollection>>(factory));
            registry.Services.Replace(ServiceDescriptor.Singleton<IServiceCollection>(registry));
            registry.MergeServices(true);

            return factory;
        }

        public virtual T GetRootService<T>() where T : class
        {
            var result = RootProvider.GetService<T>();
            return result;
        }

        public virtual IEnumerable<T> GetRootServices<T>() where T : class
        {
            return RootProvider.GetServices<T>();
        }

        public virtual T GetRequiredRootService<T>() where T : class
        {
            return RootProvider.GetRequiredService<T>();
        }

        public virtual object GetRootService(Type type)
        {
            return RootProvider.GetService(type);
        }

        public virtual bool TryGetService<T>(out T service) where T : class
        {
            return (service = GetService<T>()) == null;
        }

        public virtual T GetService<T>() where T : class
        {
            return Provider.GetService<T>();            
        }

        public virtual IEnumerable<T> GetServices<T>() where T : class
        {
            return Provider.GetServices<T>();
        }

        public virtual T GetRequiredService<T>() where T : class
        {
            return Provider.GetRequiredService<T>();
        }

        public virtual object GetService(Type type)
        {
            return Provider.GetService(type);
        }

        public virtual bool TryGetService(Type type, out object service)
        {
            return (service = GetService(type)) == null;
        }

        public virtual IEnumerable<object> GetServices(Type type)
        {
            return Provider.GetServices(type);
        }

        public Lazy<T> GetRequiredServiceLazy<T>() where T : class
        {
            return new Lazy<T>(GetRequiredService<T>, true);
        }

        public Lazy<T> GetServiceLazy<T>() where T : class
        {
            return new Lazy<T>(GetService<T>, true);
        }

        public Lazy<IEnumerable<T>> GetServicesLazy<T>() where T : class
        {
            return new Lazy<IEnumerable<T>>(GetServices<T>, true);
        }

        public virtual T GetSingleton<T>() where T : class
        {
            return GetObject<T>();
        }

        public virtual object GetSingleton(Type type)
        {
            return InnerRegistry.GetObject(type);
        }

        public virtual object GetRequiredService(Type type)
        {
            return Provider.GetRequiredService(type);
        }

        public virtual T InitializeRootService<T>(params object[] parameters) where T : class
        {
            return ActivatorUtilities.CreateInstance<T>(RootProvider, parameters);
        }

        public virtual T EnsureGetRootService<T>() where T : class
        {
            return ActivatorUtilities.GetServiceOrCreateInstance<T>(RootProvider);
        }

        public static void SetProvider(IServiceProvider serviceProvider)
        {
            var _provider = serviceProvider;
            _provider.GetRequiredService<ServiceObject<IServiceProvider>>().Value = _provider;
        }

        public IServiceManager ReplaceProvider(IServiceProvider serviceProvider)
        {
            Provider.GetRequiredService<ServiceObject<IServiceProvider>>().Value = serviceProvider;
            InnerProvider = InnerRegistry.GetProvider();
            return this;
        }

        public IServiceProvider BuildInternalProvider(bool withPropertyInjection = false)
        {
            SetProvider(GetRegistry().BuildServiceProviderFromFactory<IServiceCollection>());
            return InnerProvider = InnerRegistry.GetProvider();
        }

        public static IServiceProvider BuildInternalRootProvider(bool withPropertyInjection = false)
        {
            SetProvider(GetRootRegistry().BuildServiceProviderFromFactory<IServiceCollection>());
            return GetRootProvider();
        }

        public static IServiceProvider GetRootProvider()
        {
            var _provider = rootRegistry.GetProvider();
            if (_provider == null)
                return BuildInternalRootProvider();
            return _provider;
        }

        public IServiceProvider AddPropertyInjection()
        {
            var _provider = GetProvider() ?? GetRegistry().BuildServiceProviderFromFactory<IServiceCollection>();

            SetProvider(_provider.AddPropertyInjection());

            return _provider;
        }

        public IServiceProvider GetProvider()
        {
            return (InnerProvider ??= InnerRegistry.GetProvider()) ?? BuildInternalProvider();
        }

        public IServiceProviderFactory<IServiceCollection> GetProviderFactory()
        {
            return GetObject<IServiceProviderFactory<IServiceCollection>>();
        }

        public IServiceProvider CreateProviderFromFacotry()
        {
            return Registry.BuildServiceProviderFromFactory<IServiceCollection>();
        }

        public static IServiceProviderFactory<IServiceCollection> GetRootProviderFactory()
        {
            return GetRootObject<IServiceProviderFactory<IServiceCollection>>();
        }

        public ObjectFactory CreateFactory<T>(Type[] constrTypes)
        {
            return ActivatorUtilities.CreateFactory(typeof(T), constrTypes);
        }

        public ObjectFactory CreateFactory(Type instanceType, Type[] constrTypes)
        {
            return ActivatorUtilities.CreateFactory(instanceType, constrTypes);
        }

        public T Initialize<T>(params object[] besidesInjectedArguments)
        {
            return ActivatorUtilities.CreateInstance<T>(Provider, besidesInjectedArguments);
        }

        public object Initialize(Type type, params object[] besidesInjectedArguments)
        {
            return ActivatorUtilities.CreateInstance(Provider, type, besidesInjectedArguments);
        }

        public T EnsureGetService<T>()
        {
            return ActivatorUtilities.GetServiceOrCreateInstance<T>(Provider);
        }

        public object EnsureGetService<T>(Type type)
        {
            return ActivatorUtilities.GetServiceOrCreateInstance(Provider, type);
        }

        public T GetObject<T>() where T : class
        {
            return InnerRegistry.GetObject<T>();
        }

        public T AddObject<T>(T obj) where T : class
        {
            return InnerRegistry.AddObject(obj).Value;
        }
        public T AddObject<T>() where T : class
        {
            return InnerRegistry.AddObject(typeof(T).New<T>()).Value;
        }

        public T AddKeyedObject<T>(object key, T obj) where T : class
        {
            return InnerRegistry.AddKeyedObject(key, obj).Value;
        }
        public T AddKeyedObject<T>(object key) where T : class
        {
            return InnerRegistry.AddKeyedObject(key, typeof(T).New<T>()).Value;
        }

        public static T GetRootObject<T>() where T : class
        {
            return rootRegistry.GetObject<T>();
        }

        public static T AddRootObject<T>(T obj) where T : class
        {
            return rootRegistry.AddObject(obj).Value;
        }
        public static T AddRootObject<T>() where T : class
        {
            return rootRegistry.AddObject(typeof(T).New<T>()).Value;
        }

        public void SetInnerProvider(IServiceProvider serviceProvider)
        {
            InnerProvider = serviceProvider;
        }

        public IServiceProvider GetSession()
        {
            return (SessionScope ??= CreateScope()).ServiceProvider;
        }

        public IServicer SetTenantServicer(ClaimsPrincipal tenantUser, IServicer servicer)
        {
            IServiceManager manager = null;
            if (
              tenantUser.Identity.IsAuthenticated
                && long.TryParse(
                    tenantUser.Claims.FirstOrDefault(c => c.Type == "tenant_id")?.Value,
                    out var tenantId
                )
            )
            {
                manager = GetKeyedObject<IServiceManager>(tenantId);
                if (manager != null)
                {
                    servicer.SetManager(manager);
                    return manager.SetServicer(servicer);
                }
            }

            return GetManager().SetServicer(servicer);
        }

        public IServicer CreateServicer()
        {
            var _scope = CreateScope();
            var _servicer = _scope.ServiceProvider.GetService<IServicer>();
            _servicer.SetInnerProvider(_scope.ServiceProvider);             
            _servicer.SetScope(_scope);
            _servicer.IsScoped = true;
            return _servicer;
        }

        public IServicer SetServicer(IServicer servicer)
        {        
            var _scope = CreateScope();
            servicer.SetInnerProvider(_scope.ServiceProvider);
            servicer.SetScope(_scope);
            servicer.IsScoped = true;
            return servicer;
        }

        public IServiceProvider CreateSession()
        {         
            return CreateServicer().SessionProvider;
        }

        public IServiceScope GetScope()
        {
            return SessionScope ??= CreateScope();
        }

        public IServiceScope SetScope(IServiceScope scope)
        {
            return SessionScope = scope;       
        }

        public IServiceManager SetManager(IServiceManager serviceManager)
        {
            Manager = serviceManager;
            InnerRegistry = serviceManager.Registry;
            InnerProvider = serviceManager.Provider;
            InnerConfiguration = serviceManager.Configuration;
            Refresh();
            return this;
        }

        public static IServiceScope CreateRootScope()
        {
            return GetRootProvider().CreateScope();
        }

        public IServiceScope CreateScope()
        {
            return GetProvider().CreateScope();
        }

        public static IServiceManager GetRootManager()
        {
            return GetRootObject<IServiceManager>();
        }

        public IServiceManager GetManager()
        {
            return GetObject<IServiceManager>();
        }

        public static IServiceRegistry GetRootRegistry()
        {
            return rootRegistry;
        }

        public IServiceRegistry GetRegistry()
        {
            return InnerRegistry;
        }

        public IServiceRegistry GetRegistry(IServiceCollection services)
        {
            return InnerRegistry ??= new ServiceManager(services).Registry;
        }

        public static IServiceConfiguration GetRootConfiguration()
        {
            return rootConfiguration;
        }

        public IServiceConfiguration GetConfiguration()
        {
            return InnerConfiguration;
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (SessionScope != null)
                        SessionScope.Dispose();
                }
                disposedValue = true;
            }
        }

        public override async ValueTask DisposeAsyncCore()
        {
            if (SessionScope != null)
                await Task.Factory.StartNew(() =>
                SessionScope.Dispose()).ConfigureAwait(false);
        }

        public async Task LoadDataServiceModels()
        {
            await Task.WhenAll(GetClients().ForEach(client => client.BuildMetadata()).Commit());

            Registry.AddOpenDataRemoteImplementations();
        }

        public async Task<ServiceManager> UseServiceClients(bool buildProvider = false)
        {
            await LoadDataServiceModels();

            if (buildProvider)
                BuildInternalProvider();

            return this;
        }

        public T GetKeyedObject<T>(object key) where T : class
        {
            return InnerRegistry.GetKeyedObject<T>(key);
        }

        public T GetKeyedService<T>(object key) where T : class
        {
            return InnerRegistry.GetKeyedService<T>(key);
        }

        public T GetKeyedSingleton<T>(object key) where T : class
        {
            return InnerRegistry.GetKeyedSingleton<ServiceObject<T>>(key)?.Value;
        }
    }
}
