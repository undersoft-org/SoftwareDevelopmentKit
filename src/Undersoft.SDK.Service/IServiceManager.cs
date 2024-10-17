using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using Undersoft.SDK.Service.Configuration;
using Undersoft.SDK.Service.Data.Repository;

namespace Undersoft.SDK.Service
{
    public interface IServiceManager : IRepositoryManager, IServiceProvider
    {
        IServiceConfiguration Configuration { get; set; }
        IServiceProvider Provider { get; }
        IServiceRegistry Registry { get; }
        IServiceProvider RootProvider { get; }
        IServiceProvider SessionProvider { get; }

        object Activate(Type type, params object[] besidesInjectedArguments);
        T Activate<T>(params object[] besidesInjectedArguments);
        T AddKeyedObject<T>(object key) where T : class;
        T AddKeyedObject<T>(object key, T obj) where T : class;
        T AddObject<T>() where T : class;
        T AddObject<T>(T obj) where T : class;
        IServiceProvider AddPropertyInjection();
        IServiceProvider BuildInternalProvider(bool withPropertyInjection = false);
        IServiceProviderFactory<IServiceCollection> BuildServiceProviderFactory(IServiceRegistry registry);
        ObjectFactory CreateFactory(Type instanceType, Type[] constrTypes);
        ObjectFactory CreateFactory<T>(Type[] constrTypes);
        IServiceProvider CreateProviderFromFacotry();
        IServiceScope CreateScope();
        IServicer CreateServicer();
        T EnsureGetRootService<T>() where T : class;
        T EnsureGetService<T>();
        object EnsureGetService<T>(Type type);
        IServiceConfiguration GetConfiguration();
        IServiceManager GetKeyedManager(object key);
        T GetKeyedObject<T>(object key) where T : class;
        T GetKeyedService<T>(object key) where T : class;
        T GetKeyedSingleton<T>(object key) where T : class;
        IServiceManager GetManager();
        T GetObject<T>() where T : class;
        IServiceProvider GetProvider();
        IServiceProviderFactory<IServiceCollection> GetProviderFactory();
        IServiceRegistry GetRegistry();
        IServiceRegistry GetRegistry(IServiceCollection services);
        T GetRequiredRootService<T>() where T : class;
        object GetRequiredService(Type type);
        T GetRequiredService<T>() where T : class;
        Lazy<T> GetRequiredServiceLazy<T>() where T : class;
        object GetRootService(Type type);
        T GetRootService<T>() where T : class;
        IEnumerable<T> GetRootServices<T>() where T : class;
        IServiceScope GetScope();
        object GetService(Type type);
        T GetService<T>() where T : class;
        Lazy<T> GetServiceLazy<T>() where T : class;
        IEnumerable<object> GetServices(Type type);
        IEnumerable<T> GetServices<T>() where T : class;
        Lazy<IEnumerable<T>> GetServicesLazy<T>() where T : class;
        IServiceProvider GetSessionProvider();
        object GetSingleton(Type type);
        T GetSingleton<T>() where T : class;
        void Initialize();
        void Initialize(IServiceCollection services);
        void Initialize(IServiceCollection services, IConfiguration configuration);
        T InitializeRootService<T>(params object[] parameters) where T : class;
        Task LoadDataServiceModels();
        IServiceManager ReplaceProvider(IServiceProvider serviceProvider);
        void SetInnerProvider(IServiceProvider serviceProvider);
        IServiceManager SetManager(IServiceManager serviceManager);
        IServiceScope SetScope(IServiceScope scope);
        IServicer SetServicer(IServicer servicer);
        IServicer SetTenantServicer(ClaimsPrincipal tenantUser, IServicer servicer);
        bool TryGetKeyedManager(object key, out IServiceManager manager);
        bool TryGetKeyedObject<T>(object key, out T output) where T : class;
        bool TryGetKeyedService<T>(object key, out T output) where T : class;
        bool TryGetKeyedSingleton<T>(object key, out T output) where T : class;
        bool TryGetObject<T>(out T output) where T : class;
        bool TryGetService(Type type, out object service);
        bool TryGetService<T>(out T service) where T : class;
        Task<ServiceManager> UseServiceClients(bool buildProvider = false);

    }
}