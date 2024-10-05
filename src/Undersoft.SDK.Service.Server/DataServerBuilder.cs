using Microsoft.EntityFrameworkCore;

namespace Undersoft.SDK.Service.Server
{
    public abstract class DataServerBuilder : Registry<Type>, IDataServerBuilder
    {
        protected IServiceRegistry ServiceRegistry { get; set; }
        public StoreRouteRegistry RouteRegistry { get; set; }
        public virtual Type StoreType { get; set; }
        public string RoutePrefix { get; set; } = "";

        public int PageLimit { get; set; } = 10000;

        protected ISeries<Type> ContextTypes { get; set; } = new Catalog<Type>();

        public DataServerBuilder(string providerPrefix, Type storeType, IServiceRegistry registry) : base()
        {
            ServiceRegistry = registry;
            RouteRegistry = ServiceRegistry.GetObject<StoreRouteRegistry>();
            StoreType = storeType;
            RoutePrefix = GetRoute(providerPrefix);
        }

        public DataServerBuilder(Type storeType, IServiceRegistry registry) : this(null, storeType, registry)
        {
          
        }

        public abstract void Build();

        protected virtual string GetRoute(string providerPrefix = null)
        {
            string route = null;
            if (RouteRegistry != null)
            {
                if (RouteRegistry.TryGet(StoreType, out (Type, string) routeEntry))
                    route = routeEntry.Item2;
            }

            switch (StoreType)
            {
                case IEventStore:
                    route = StoreRoutes.EventStore;
                    break;
                case IAccountStore:
                    route = StoreRoutes.AccountStore;
                    break;
                default:
                    route = StoreRoutes.DataStore;
                    break;
            };
            return $"{providerPrefix}/{route}";
        }

        public virtual IDataServerBuilder AddDataServices<TContext>() where TContext : DbContext
        {
            TryAdd(typeof(TContext));
            return this;
        }

        public virtual IDataServerBuilder AddInvocations<TAuth>() where TAuth : class
        {
            return this;
        }

    }

    public enum DataServerTypes
    {
        None = 0,
        Grpc = 1,
        OData = 2,
        Rest = 4,
        All = 7
    }
}
