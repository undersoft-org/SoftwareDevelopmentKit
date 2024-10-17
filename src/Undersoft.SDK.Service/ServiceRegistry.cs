using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Undersoft.SDK.Service
{

    public partial class ServiceRegistry : Registry<ServiceDescriptor>, IServiceRegistry
    {
        public ServiceRegistry() : base(true)
        {
            AddObject<IServiceProvider>();
            AddObject<IServiceRegistry>(this);
        }

        public ServiceRegistry(IServiceCollection services, IServiceManager manager) : this()
        {
            Services = services;
            AddObject(manager);
        }

        public IServiceCollection Services { get; set; }
        public IServiceManager Manager => GetManager();

        public ServiceDescriptor this[string name]
        {
            get => base[GetKey(name)];
            set => base.Set(GetKey(name), value);
        }
        public ServiceDescriptor this[Type serviceType]
        {
            get => base[GetKey(serviceType)];
            set => base.Set(GetKey(serviceType), value);
        }
        public ServiceDescriptor this[object key, Type serviceType]
        {
            get => base[GetKey(key, serviceType)];
            set => base.Set(GetKey(key, serviceType), value);
        }

        public ServiceDescriptor Get(Type contextType)
        {
            return base.Get(GetKey(contextType));
        }
        public ServiceDescriptor Get(object key, Type contextType)
        {
            return base.Get(GetKey(key, contextType));
        }
        public ServiceDescriptor Get<TService>() where TService : class
        {
            return base.Get(GetKey<TService>());
        }
        public ServiceDescriptor Get<TService>(object key) where TService : class
        {
            return base.Get(GetKey<TService>(key));
        }
        public override ServiceDescriptor Get(object key)
        {
            return base.Get(GetKey(key));
        }

        public override bool TryGet(object key, out ServiceDescriptor value)
        {
            if (base.TryGet(GetKey(key), out value))
                return true;
            return false;
        }
        public bool TryGet<TService>(object key, out ServiceDescriptor value) where TService : class
        {
            if (base.TryGet(GetKey<TService>(key), out value))
                return true;
            return false;
        }
        public bool TryGet(object key, Type type, out ServiceDescriptor value)
        {
            if (base.TryGet(GetKey(key, type), out value))
                return true;
            return false;
        }
        public bool TryGet<TService>(out ServiceDescriptor profile) where TService : class
        {
            if (base.TryGet(GetKey<TService>(), out profile))
                return true;
            return false;
        }

        public override bool TryAdd(ServiceDescriptor profile)
        {
            long key = GetKey(profile);
            if (ContainsKey(key))
                return false;
            base.Add(key, profile);
            return true;
        }

        public bool Remove<TContext>() where TContext : class
        {
            return TryRemove(typeof(TContext));
        }

        protected override bool InnerAdd(ServiceDescriptor value)
        {
            return base.InnerAdd(value.ServiceKey != null ? GetKey(value.ServiceKey, value.ServiceType) : GetKey(value.ServiceType), value);
        }

        protected override ISeriesItem<ServiceDescriptor> InnerPut(ServiceDescriptor value)
        {
            return base.InnerPut(value.ServiceKey != null ? GetKey(value.ServiceKey, value.ServiceType) : GetKey(value.ServiceType), value);
        }

        public override ISeriesItem<ServiceDescriptor> Set(ServiceDescriptor descriptor)
        {
            return base.Set(descriptor.ServiceKey != null ? GetKey(descriptor.ServiceKey, descriptor.ServiceType) : GetKey(descriptor.ServiceType), descriptor);
        }

        public override void Add(ServiceDescriptor item)
        {
            base.Add(item);
        }

        public long GetKey(object key, Type type)
        {
            return key.UniqueKey64(type.UniqueKey64());
        }
        public long GetKey(ServiceDescriptor value)
        {
            return value.ServiceKey != null ? GetKey(value.ServiceKey, value.ServiceType) : GetKey(value.ServiceType);
        }
        public long GetKey(Type item)
        {
            return item.UniqueKey64();
        }
        public long GetKey(object item)
        {
            return item.UniqueKey64();
        }
        public long GetKey<T>()
        {
            return typeof(T).UniqueKey64();
        }
        public long GetKey<T>(object key)
        {
            return key.UniqueKey64(typeof(T).UniqueKey64());
        }      

        public override int IndexOf(ServiceDescriptor item)
        {
            return base.IndexOf(GetKey(item), item);
        }

        public override void Insert(int index, ServiceDescriptor item)
        {
            base.Insert(index, GetItem(GetKey(item), item));
        }

        public override bool Contains(ServiceDescriptor item)
        {
            return base.Contains(GetKey(item), item);
        }

        public override void CopyTo(ServiceDescriptor[] array, int arrayIndex)
        {
            base.CopyTo(array, arrayIndex);
        }

        public override bool Remove(ServiceDescriptor item)
        {
            return base.TryRemove(GetKey(item.ServiceType), item);
        }

        public bool ContainsKey<TService>()
        {
            return base.ContainsKey(GetKey<TService>());
        }
        public bool ContainsKey<TService>(object key)
        {
            return base.ContainsKey(GetKey<TService>(key));
        }
        public bool ContainsKey(Type type)
        {
            return base.ContainsKey(GetKey(type));
        }
        public override bool ContainsKey(object key)
        {
            return base.ContainsKey(GetKey(key));
        }
        public bool ContainsKey(object key, Type type)
        {
            return base.ContainsKey(GetKey(key, type));
        }

        public IServiceRegistry ReplaceServices(IServiceCollection services)
        {
            services.Replace(ServiceDescriptor.Singleton<IServiceCollection>(this));
            return this;
        }

        public void MergeServices(bool updateSourceServices = true)
        {
            MergeServices(Services, updateSourceServices);
        }
        public void MergeServices(IServiceCollection sourceServices, bool updateSourceServices = true)
        {
            if (sourceServices.Count == Count)
                return;

            if (!updateSourceServices)
                sourceServices.ForEach(s => { if (!Contains(s)) Add(s); });
            else
            {
                var tempRegistry = new Registry<ServiceDescriptor>(true);

                sourceServices.ForEach(s =>
                {
                    tempRegistry.Add(GetKey(s.ServiceType), s);
                    if (!Contains(s))
                        Add(s);
                });

                this.ForEach(c =>
                {
                    if (!tempRegistry.Contains(GetKey(c.ServiceType), c))
                        sourceServices.Add(c);
                });
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (var service in this)
                        service.Dispose();
                    base.Dispose(true);
                }
                disposedValue = true;
            }
        }
    }
}
