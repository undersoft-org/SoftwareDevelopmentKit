namespace Undersoft.SDK.Proxies;

using Uniques;

public static class ProxyFactory
{
    public static IProxy CreateProxy(object item)
    {
        return TryCreateInnerProxy(item, item.GetType());
    }

    public static IProxy CreateProxy<T>(T item)
    {
        return TryCreateInnerProxy(item, typeof(T));
    }

    public static IProxy CreateProxy<T>(object item)
    {
        return TryCreateInnerProxy(item, typeof(T));
    }

    private static IProxy TryCreateInnerProxy(object item, Type type)
    {
        if (!TryGetInnerProxy(item, type, out var proxy))
        {
            var key = type.UniqueKey32();
            if (!ProxyGeneratorFactory.Cache.TryGet(key, out ProxyGenerator _proxy))
                ProxyGeneratorFactory.Cache.Add(key, _proxy = new ProxyGenerator(type));

            return _proxy.Generate(item);
        }
        return proxy;
    }

    private static bool TryGetInnerProxy(object item, Type type, out IProxy proxy)
    {
        var t = type;
        if (t.IsAssignableTo(typeof(IProxy)))
        {
            proxy = (IProxy)item;
            return true;
        }
        else if (t.IsAssignableTo(typeof(IInnerProxy)))
        {
            proxy = ((IInnerProxy)item).Proxy;
            if (proxy != null)
                return true;
        }
        proxy = null;
        return false;
    }
}
