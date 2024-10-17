using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Undersoft.SDK.Utilities;

namespace Undersoft.SDK.Service;

public partial class ServiceRegistry
{
    public ServiceObject<T> EnsureGetObject<T>() where T : class
    {        
        if (!TryGet<ServiceObject<T>>(out var output))
            return AddObject<T>();
        return (ServiceObject<T>)output.ImplementationInstance;
    }

    public ServiceObject EnsureGetObject(Type type)
    {
        Type accessorType = typeof(ServiceObject<>).MakeGenericType(type);
        if (!TryGet(accessorType,  out ServiceDescriptor output))
            return AddObject(accessorType);
        return (ServiceObject)output.ImplementationInstance;
    }

    public ServiceObject<T> AddKeyedObject<T>(object key) where T : class
    {
        return AddKeyedObject(key, new ServiceObject<T>());
    }

    public ServiceObject<T> AddObject<T>() where T : class
    {
        return AddObject(new ServiceObject<T>());
    }

    public ServiceObject AddKeyedObject(object key, Type type)
    {
        return AddKeyedObject(key, type, null);
    }

    public ServiceObject AddObject(Type type)
    {
        return AddObject(type, null);
    }

    public ServiceObject AddObject(Type type, object obj)
    {
        Type oaType = typeof(ServiceObject<>).MakeGenericType(type);
        Type iaType = typeof(IServiceObject<>).MakeGenericType(type);

        ServiceObject accessor = (ServiceObject)oaType.New(obj);

        if (!ContainsKey(oaType))
        {
            Put(ServiceDescriptor.Singleton(oaType, accessor));
            Put(ServiceDescriptor.Singleton(iaType, accessor));
        }        

        if (!ContainsKey(type) && obj != null)
            Put(ServiceDescriptor.Singleton(type, accessor.Value));

        return accessor;
    }

    public ServiceObject AddKeyedObject(object key, Type type, object obj)
    {
        Type oaType = typeof(ServiceObject<>).MakeGenericType(type);
        Type iaType = typeof(IServiceObject<>).MakeGenericType(type);

        ServiceObject accessor = (ServiceObject)oaType.New(obj);

        if (!ContainsKey(GetKey(key, oaType)))
        {
            Put(ServiceDescriptor.KeyedSingleton(oaType, key, accessor));
            Put(ServiceDescriptor.KeyedSingleton(iaType, key, accessor));
        }

        if (!ContainsKey(GetKey(key, type)) && obj != null)
            Put(ServiceDescriptor.KeyedSingleton(type, key, accessor.Value));

        return accessor;
    }

    public ServiceObject<T> AddObject<T>(T obj) where T : class
    {
        return AddObject(new ServiceObject<T>(obj));
    }

    public ServiceObject<T> AddKeyedObject<T>(object key, T obj) where T : class
    {
        return AddKeyedObject<T>(key, new ServiceObject<T>(obj));
    }

    public ServiceObject<T> AddObject<T>(ServiceObject<T> accessor) where T : class
    {
        if (!ContainsKey<ServiceObject<T>>())
        {
            Put(ServiceDescriptor.Singleton(accessor));
            Put(ServiceDescriptor.Singleton<IServiceObject<T>>(accessor));
        }

        if (!ContainsKey<T>() && accessor.Value != null)
            Put(ServiceDescriptor.Singleton(accessor.Value));

        return accessor;
    }

    public ServiceObject<T> AddKeyedObject<T>(object key, ServiceObject<T> accessor) where T : class
    {
        if (!ContainsKey<ServiceObject<T>>(key))
        {
            Put(ServiceDescriptor.KeyedSingleton(key, accessor));
            Put(ServiceDescriptor.KeyedSingleton<IServiceObject<T>>(key, accessor));            
        }

        if (!ContainsKey<T>(key) && accessor.Value != null)
            Put(ServiceDescriptor.KeyedSingleton(key, accessor.Value));

        return accessor;
    }

    public void SetObject<T>(T obj) where T : class
    {
        if (ContainsKey<ServiceObject<T>>())
            ((ServiceObject<T>)Get<ServiceObject<T>>().ImplementationInstance).Value = obj;
    }

    public void SetKeyedObject<T>(object key, T obj) where T : class
    {
        if (ContainsKey<ServiceObject<T>>(key))
            ((ServiceObject<T>)Get<ServiceObject<T>>(key).KeyedImplementationInstance).Value = obj;
    }

    public void ReplaceObject<T>(T obj) where T : class
    {
        if (ContainsKey<ServiceObject<T>>())        
            ((ServiceObject<T>)Get<ServiceObject<T>>().ImplementationInstance).Value = obj;        

        if (ContainsKey<T>() && obj != null)        
            Put(ServiceDescriptor.Singleton(typeof(T), obj));        
    }

    public void ReplaceKeyedObject<T>(object key, T obj) where T : class
    {
        if (ContainsKey<ServiceObject<T>>(key))
            ((ServiceObject<T>)Get<ServiceObject<T>>(key).KeyedImplementationInstance).Value = obj;

        if (ContainsKey<T>(key) && obj != null)
            Put(ServiceDescriptor.KeyedSingleton(key, obj));
    }

    public object GetObject(Type type)
    {
        Type accessorType = typeof(ServiceObject<>).MakeGenericType(type);
        return ((ServiceObject)GetSingleton(accessorType))?.Value;
    }

    public T GetObject<T>()
        where T : class
    {
        return GetSingleton<ServiceObject<T>>()?.Value;
    }

    public bool TryGetObject<T>(out T output) where T : class
    {
        return (output = GetObject<T>()) != null;
    }

    public T GetKeyedObject<T>(object key) where T : class
    {
        return GetKeyedSingleton<ServiceObject<T>>(key)?.Value;
    }

    public bool TryGetKeyedObject<T>(object key, out T output) where T : class
    {
        return (output = GetKeyedObject<T>(key)) != null; 
    }

    public T GetRequiredObject<T>()
        where T : class
    {
        return GetObject<T>() ?? throw new Exception($"Could not find an object of {typeof(T).AssemblyQualifiedName} in  Be sure that you have used AddObjectAccessor before!");
    }
}