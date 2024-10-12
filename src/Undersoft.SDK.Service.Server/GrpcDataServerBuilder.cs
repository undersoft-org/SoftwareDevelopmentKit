using Microsoft.Extensions.DependencyInjection;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Grpc.Server;
using System.Reflection;

namespace Undersoft.SDK.Service.Server;

using Undersoft.SDK.Service.Data.Client.Attributes;
using Undersoft.SDK.Service.Data.Contract;
using Undersoft.SDK.Service.Data.Store;
using Undersoft.SDK.Service.Server.Controller.Stream.Abstractions;

public class GrpcDataServerBuilder<TServiceStore>
    : DataServerBuilder,
        IDataServerBuilder<TServiceStore> where TServiceStore : IDataStore
{
    static bool grpcadded = false;

    public GrpcDataServerBuilder(IServiceRegistry registry) : base("stream", typeof(TServiceStore), registry)
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

        foreach (var controllerType in controllerTypes)
        {
            Type contractType = null;

            var genTypes = controllerType.BaseType.GenericTypeArguments;

            if (
                genTypes.Length > 4
                && genTypes[1].IsAssignable(StoreType)
                && genTypes[2].IsAssignable(StoreType)
            )
                contractType = typeof(IStreamDataController<>).MakeGenericType(new[] { genTypes[4] });
            else if (genTypes.Length > 3)
                if (
                    genTypes[3].IsAssignableTo(typeof(IContract))
                    && genTypes[1].IsAssignable(StoreType)
                )
                    contractType = typeof(IStreamDataController<>).MakeGenericType(
                        new[] { genTypes[3] }
                    );
                else
                    continue;

            GrpcDataServerRegistry.ServiceContracts.Add(contractType);

            ServiceRegistry.AddSingleton(contractType, controllerType);
        }
    }

    public override void Build()
    {
        AddControllers();
        ServiceRegistry.MergeServices(true);
    }   

    public virtual void AddGrpcServicer()
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
                BinderConfiguration.Create(binder: new GrpcDataServerBinder(ServiceRegistry))
            );

            ServiceRegistry.AddCodeFirstGrpcReflection();

            ServiceRegistry.MergeServices(true);

            grpcadded = true;
        }
    }
}
