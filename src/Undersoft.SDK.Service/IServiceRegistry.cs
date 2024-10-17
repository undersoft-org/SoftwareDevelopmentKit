using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace Undersoft.SDK.Service
{
    public interface IServiceRegistry : IServiceCollection
    {
        ServiceDescriptor this[string name] { get; set; }
        ServiceDescriptor this[Type serviceType] { get; set; }
        ServiceDescriptor this[object key, Type serviceType] { get; set; }

        IServiceManager Manager { get; }
        IServiceCollection Services { get; set; }
        
        ServiceObject AddKeyedObject(object key, Type type);
        ServiceObject AddKeyedObject(object key, Type type, object obj);
        ServiceObject<T> AddKeyedObject<T>(object key) where T : class;
        ServiceObject<T> AddKeyedObject<T>(object key, ServiceObject<T> accessor) where T : class;
        ServiceObject<T> AddKeyedObject<T>(object key, T obj) where T : class;
        ServiceObject AddObject(Type type);
        ServiceObject AddObject(Type type, object obj);
        ServiceObject<T> AddObject<T>() where T : class;
        ServiceObject<T> AddObject<T>(ServiceObject<T> accessor) where T : class;
        ServiceObject<T> AddObject<T>(T obj) where T : class;
        IServiceProvider BuildServiceProviderFromFactory();
        IServiceProvider BuildServiceProviderFromFactory<TContainerBuilder>([NotNull] Action<TContainerBuilder> builderAction = null);        
        bool ContainsKey(object key);
        bool ContainsKey(object key, Type type);
        bool ContainsKey(Type type);
        bool ContainsKey<TService>();
        bool ContainsKeyedService(object key, Type type);
        bool ContainsKeyedService<T>(object key);
        bool ContainsService(Type type);
        bool ContainsService<T>() where T : class;        
        ServiceObject EnsureGetObject(Type type);
        ServiceObject<T> EnsureGetObject<T>() where T : class;
        ServiceDescriptor Get(object key);
        ServiceDescriptor Get(object key, Type contextType);
        ServiceDescriptor Get(Type contextType);
        ServiceDescriptor Get<TService>() where TService : class;
        ServiceDescriptor Get<TService>(object key) where TService : class;
        long GetKey(object item);
        long GetKey(object key, Type type);
        long GetKey(ServiceDescriptor value);
        long GetKey(Type item);
        long GetKey<T>();
        long GetKey<T>(object key);
        IServiceManager GetManager();
        IServiceManager GetKeyedManager(object key);
        bool TryGetKeyedManager(object key, out IServiceManager manager);
        T GetKeyedObject<T>(object key) where T : class;
        T GetKeyedService<T>(object key) where T : class;
        T GetKeyedSingleton<T>(object key) where T : class;
        object GetObject(Type type);
        T GetObject<T>() where T : class;
        IServiceProvider GetProvider();
        T GetRequiredObject<T>() where T : class;
        object GetRequiredService(Type type);
        T GetRequiredService<T>() where T : class;
        Lazy<object> GetRequiredServiceLazy(Type type);
        Lazy<T> GetRequiredServiceLazy<T>() where T : class;
        T GetRequiredSingleton<T>() where T : class;
        object GetService(Type type);
        T GetService<T>() where T : class;
        Lazy<object> GetServiceLazy(Type type);
        Lazy<T> GetServiceLazy<T>() where T : class;
        object GetSingleton(Type type);
        T GetSingleton<T>() where T : class;        
        void MergeServices(bool updateSourceServices = true);
        void MergeServices(IServiceCollection sourceServices, bool updateSourceServices = true);
        bool Remove<TContext>() where TContext : class;
        void ReplaceKeyedObject<T>(object key, T obj) where T : class;
        void ReplaceObject<T>(T obj) where T : class;
        IServiceRegistry ReplaceServices(IServiceCollection services);
        ISeriesItem<ServiceDescriptor> Set(ServiceDescriptor descriptor);
        void SetKeyedObject<T>(object key, T obj) where T : class;
        void SetObject<T>(T obj) where T : class;
        bool TryAdd(ServiceDescriptor profile);
        bool TryGet(object key, out ServiceDescriptor value);
        bool TryGet(object key, Type type, out ServiceDescriptor value);
        bool TryGet<TService>(object key, out ServiceDescriptor value) where TService : class;
        bool TryGet<TService>(out ServiceDescriptor profile) where TService : class;
        bool TryGetKeyedObject<T>(object key, out T output) where T : class;
        bool TryGetKeyedSingleton<T>(object key, out T output) where T : class;
        bool TryGetObject<T>(out T output) where T : class;
        bool TryGetSingleton<T>(out T output) where T : class;
    }
}