using ProtoBuf.Grpc.Configuration;
using System.Reflection;
using Undersoft.SDK.Service.Server.Controller;

namespace Undersoft.SDK.Service.Server.Builders
{
    internal class StreamServiceBinder : ServiceBinder
    {
        private readonly IServiceRegistry registry;

        public StreamServiceBinder(IServiceRegistry registry)
        {
            this.registry = registry;
        }

        public override IList<object> GetMetadata(MethodInfo method, Type contractType, Type serviceType)
        {
            var resolvedServiceType = serviceType;
            if (serviceType.IsInterface)
                resolvedServiceType = registry.Get(serviceType)?.ImplementationType ?? serviceType;

            return base.GetMetadata(method, contractType, resolvedServiceType);
        }

        protected override string GetDefaultName(Type contractType)
        {
            var fullname = base.GetDefaultName(contractType);
            var chunks = fullname.Split('_');
            return chunks[0].Replace("StreamController", chunks[1]);
        }
    }
}