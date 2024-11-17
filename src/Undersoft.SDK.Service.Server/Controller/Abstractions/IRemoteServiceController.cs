using Microsoft.AspNetCore.Mvc;

namespace Undersoft.SDK.Service.Server.Controller.Abstractions;
public interface IRemoteServiceController<TStore, TService, TDto>
    where TService : class
    where TDto : class
    where TStore : IDataServiceStore
{
    IActionResult Service([FromBody] IDictionary<string, Arguments> args);   
}
