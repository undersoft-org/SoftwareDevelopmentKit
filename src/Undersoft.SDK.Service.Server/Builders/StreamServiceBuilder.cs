using Microsoft.Extensions.DependencyInjection;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Grpc.Server;
using System.Reflection;

namespace Undersoft.SDK.Service.Server.Builders;

using Undersoft.SDK.Service.Data.Client.Attributes;
using Undersoft.SDK.Service.Data.Contract;
using Undersoft.SDK.Service.Data.Store;
using Undersoft.SDK.Service.Server.Controller.Abstractions;

public class StreamServiceBuilder<TServiceStore>
    : DataServerBuilder,
        IDataServerBuilder<TServiceStore> where TServiceStore : IDataStore
{
    static bool grpcadded = false;

    public StreamServiceBuilder(IServiceRegistry registry) : base(typeof(TServiceStore), registry)
    {
    }

    public void AddControllers()
    {
        Assembly[] asm = AppDomain.CurrentDomain.GetAssemblies();
        var controllerTypes = asm.SelectMany(
                a =>
                    a.GetTypes()
                        .Where(type => type.GetCustomAttribute<StreamDataAttribute>() != null)
                        .ToArray()
            )
            .Where(
                b =>
                    !b.IsAbstract
                    && b.BaseType.IsGenericType
                    && b.BaseType.GenericTypeArguments.Length > 3
            )
            .ToArray();

        var registry = new Registry<Type>();

        foreach (var controllerType in controllerTypes)
        {
            Type contractType = null;

            var genTypes = controllerType.BaseType.GenericTypeArguments;

            if (
                genTypes.Length > 4
                && genTypes[1].IsAssignable(StoreType)
                && genTypes[2].IsAssignable(StoreType)
            )
                contractType = typeof(IStreamController<>).MakeGenericType(new[] { genTypes[4] });
            else if (genTypes.Length > 3)
                if (
                    genTypes[3].IsAssignableTo(typeof(IContract))
                    && genTypes[1].IsAssignable(StoreType)
                )
                    contractType = typeof(IStreamController<>).MakeGenericType(
                        new[] { genTypes[3] }
                    );
                else
                    continue;

            registry.Add(contractType);

            ServiceRegistry.AddSingleton(contractType, controllerType);
        }

        DataServerRegistry.StreamControllers.Put(StoreType, registry);
    }

    public override void Build()
    {
        AddControllers();
        ServiceRegistry.MergeServices(true);
    }

    public virtual void AddStreamServicer()
    {
        if (!grpcadded)
        {
            ServiceRegistry
                .AddCodeFirstGrpc(config =>
                {
                    config.ResponseCompressionLevel = System
                        .IO
                        .Compression
                        .CompressionLevel
                        .Optimal;
                });

            ServiceRegistry.AddSingleton(
                BinderConfiguration.Create(binder: new StreamServiceBinder(ServiceRegistry))
            );

            ServiceRegistry.AddCodeFirstGrpcReflection();

            ServiceRegistry.MergeServices(true);

            grpcadded = true;
        }
    }
}
