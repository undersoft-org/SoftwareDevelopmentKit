using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using Undersoft.SDK.Service.Data.Repository;
using Undersoft.SDK.Service.Hosting;
using Undersoft.SDK.Utilities;

namespace Undersoft.SDK.Service
{
    using Configuration;

    public class ServiceManager : RepositoryManager, IServiceManager, IAsyncDisposable
    {
        private bool disposedValue;

        private static IServiceRegistry rootRegistry;

        private static IServiceConfiguration rootConfiguration;

        public IServiceProvider RootProvider => GetRootProvider();

        protected virtual IServiceProvider InnerProvider { get; set; }

        public IServiceProvider Provider => GetProvider();

        protected virtual IServiceScope SessionScope { get; set; }

        public IServiceProvider SessionProvider => GetSessionProvider();

        protected virtual IServiceConfiguration InnerConfiguration { get; set; }

        public IServiceConfiguration Configuration
        {
            get => InnerConfiguration;
            set => InnerConfiguration = value;
        }

        protected virtual IServiceRegistry InnerRegistry { get; set; }

        public IServiceRegistry Registry => InnerRegistry;

        public virtual bool IsScoped { get; set; }

        static ServiceManager()
        {
            var sm = new ServiceManager(null, null);
            rootRegistry = sm.Registry;
            rootConfiguration = sm.Configuration;
        }

        public ServiceManager() : base()
        {
            Manager = this;
        }

        public ServiceManager(IServiceManager serviceManager) : base(serviceManager)
        {
            InnerRegistry = serviceManager.Registry;
            InnerProvider = serviceManager.Provider;
            InnerConfiguration = serviceManager.Configuration;
        }

        internal ServiceManager(IServiceCollection services, IConfiguration configuration) : this()
        {
            Initialize(services, configuration);
        }

        public virtual void Initialize() => Initialize(null, null);
        public virtual void Initialize(IServiceCollection services) => Initialize(services, null);
        public virtual void Initialize(IServiceCollection services, IConfiguration configuration)
        {
            if (services == null)
                services = new ServiceCollection();

            InnerRegistry = new ServiceRegistry(services, this);

            BuildServiceProviderFactory(InnerRegistry);

            if (configuration != null)
                InnerConfiguration = new ServiceConfiguration(configuration, InnerRegistry);
            else
                InnerConfiguration = new ServiceConfiguration(InnerRegistry);

            AddObject(InnerConfiguration);
        }

        public virtual IServiceProviderFactory<IServiceCollection> BuildServiceProviderFactory(IServiceRegistry registry)
        {
            var factory = new ServiceManagerFactory(this);

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
            return (service = GetService<T>()) != null;
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
            return (service = GetService(type)) != null;
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
            return InnerRegistry.GetSingleton<T>();
        }

        public virtual object GetSingleton(Type type)
        {
            return InnerRegistry.GetSingleton(type);
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

        public T Activate<T>(params object[] besidesInjectedArguments)
        {
            return ActivatorUtilities.CreateInstance<T>(Provider, besidesInjectedArguments);
        }
        public object Activate(Type type, params object[] besidesInjectedArguments)
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

        public bool TryGetObject<T>(out T output) where T : class
        {
            return InnerRegistry.TryGetObject(out output);
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

        public IServiceProvider GetSessionProvider()
        {
            return (SessionScope ??= CreateScope()).ServiceProvider;
        }

        public IServicer SetTenantServicer(ClaimsPrincipal tenantUser, IServicer servicer)
        {
            if (
              tenantUser.Identity.IsAuthenticated
                && long.TryParse(
                    tenantUser.Claims.FirstOrDefault(c => c.Type == "tenant_id")?.Value,
                    out var tenantId
                )
            )
            {
                var manager = GetKeyedManager(tenantId);
                if (manager != null)
                    return servicer.SetManager(manager).SetServicer(servicer);
            }

            return GetManager().SetServicer(servicer);
        }
        public IServicer SetTenantServicer(long tenantId, IServicer servicer)
        {
            var manager = GetKeyedManager(tenantId);
            if (manager != null)
                return servicer.SetManager(manager).SetServicer(servicer);
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

        public IServicer SetServicer()
        {
            if (!GetType().IsAssignableTo(typeof(IServicer)))
                return null;
            var _scope = CreateScope();
            SetInnerProvider(_scope.ServiceProvider);
            SetScope(_scope);
            IsScoped = true;
            return (IServicer)this;
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
            RefreshClients();
            RefreshSources();
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
            return InnerRegistry.GetManager();
        }

        public IServiceManager GetKeyedManager(object key)
        {
            return InnerRegistry.GetKeyedManager(key);
        }

        public bool TryGetKeyedManager(object key, out IServiceManager manager)
        {
            return InnerRegistry.TryGetKeyedManager(key, out manager);
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
            return InnerRegistry ??= new ServiceManager(services, null).Registry;
        }

        public static IServiceConfiguration GetRootConfiguration()
        {
            return rootConfiguration;
        }

        public IServiceConfiguration GetConfiguration()
        {
            return InnerConfiguration;
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
            return Provider.GetKeyedService<T>(key);
        }

        public T GetKeyedSingleton<T>(object key) where T : class
        {
            return GetKeyedObject<T>(key);
        }

        public bool TryGetKeyedObject<T>(object key, out T output) where T : class
        {
            return InnerRegistry.TryGetKeyedObject(key, out output);
        }

        public bool TryGetKeyedService<T>(object key, out T output) where T : class
        {
            return (output = Provider.GetKeyedService<T>(key)) != null;
        }

        public bool TryGetKeyedSingleton<T>(object key, out T output) where T : class
        {
            return TryGetKeyedObject(key, out output);
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

        protected override async ValueTask DisposeAsyncCore()
        {
            await new ValueTask(Task.Run(() =>
            {
                if (SessionScope != null)
                    SessionScope.Dispose();

            }));
        }
    }
}
