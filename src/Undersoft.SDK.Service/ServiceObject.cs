namespace Undersoft.SDK.Service
{
    public class ServiceObject<T> : ServiceObject, IServiceObject<T>
    {
        public virtual new T Value
        {
            get => (T)base.Value;
            set => base.Value = value;
        }

        public ServiceObject() { }

        public ServiceObject(T obj)
        {
            Value = obj;
        }
    }

    public class ServiceObject : IServiceObject
    {
        public virtual object Value { get; set; }

        public ServiceObject() { }

        public ServiceObject(object obj)
        {
            Value = obj;
        }
    }
}
