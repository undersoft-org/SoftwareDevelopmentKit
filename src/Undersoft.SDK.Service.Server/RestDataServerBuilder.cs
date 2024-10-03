using System.Reflection;

namespace Undersoft.SDK.Service.Server;

using Undersoft.SDK.Service.Data.Client.Attributes;
using Undersoft.SDK.Service.Data.Object;
using Undersoft.SDK.Service.Data.Store;
using Undersoft.SDK.Service.Server.Controller.Api;

public class RestDataServerBuilder<TStore> : DataServerBuilder, IDataServerBuilder<TStore> where TStore : IDataStore
{
    IServiceRegistry _registry;
    protected StoreRoutesOptions storeRoutes;

    public RestDataServerBuilder(IServiceRegistry registry) : base()
    {
        _registry = registry;
        StoreType = typeof(TStore);
        storeRoutes = _registry.GetObject<StoreRoutesOptions>();
    }

    public void AddControllers()
    {

        Assembly[] asm = AppDomain.CurrentDomain.GetAssemblies();
        var controllerTypes = asm.SelectMany(
                a =>
                    a.GetTypes()
                        .Where(
                            type => type.GetCustomAttribute<ApiDataAttribute>()
                                    != null
                        )
                        .ToArray())
            .Where(
                b =>
                    !b.IsAbstract
                    && b.BaseType.IsGenericType
                    && b.BaseType.GenericTypeArguments.Length > 3
            ).ToArray();

        foreach (var controllerType in controllerTypes)
        {
            Type ifaceType = null;
            var genTypes = controllerType.BaseType.GenericTypeArguments;

            if (genTypes.Length > 4 && genTypes[1].IsAssignable(StoreType) && genTypes[2].IsAssignable(StoreType))
                ifaceType = typeof(IApiDataController<,,>).MakeGenericType(new[] { genTypes[0], genTypes[3], genTypes[4] });
            else if (genTypes.Length > 3)
                if (genTypes[3].IsAssignableTo(typeof(IDataObject)) && genTypes[1].IsAssignable(StoreType))
                    ifaceType = typeof(IApiDataController<,,>).MakeGenericType(new[] { genTypes[0], genTypes[2], genTypes[3] });
                else
                    continue;
        }
    }

    public override void Build()
    {
        AddControllers();
        _registry.MergeServices(true);
    }

    protected override string GetRoutes()
    {
        if (storeRoutes != null)
        {
            var route = storeRoutes.ValueOf(StoreType.Name.Substring(1))?.ToString();
            if (route != null)
                return route;
        }

        if (StoreType == typeof(IEventStore))
        {
            return storeRoutes?.ApiEventRoute ?? StoreRoutes.ApiEventRoute;
        }
        else
        {
            return storeRoutes?.ApiDataRoute ?? StoreRoutes.ApiDataRoute;
        }
    }
}