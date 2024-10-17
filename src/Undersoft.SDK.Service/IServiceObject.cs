namespace Undersoft.SDK.Service
{
    public interface IServiceObject<out T> : IServiceObject 
    {
        new T Value { get; }
    }

    public interface IServiceObject
    {
        object Value { get; }
    }
}