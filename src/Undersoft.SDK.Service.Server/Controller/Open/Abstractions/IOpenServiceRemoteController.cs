namespace Undersoft.SDK.Service.Server.Controller.Open.Abstractions;
public interface IOpenServiceRemoteController<TStore, TService, TDto>
    where TService : class
    where TDto : class
    where TStore : IDataServiceStore
{
}
