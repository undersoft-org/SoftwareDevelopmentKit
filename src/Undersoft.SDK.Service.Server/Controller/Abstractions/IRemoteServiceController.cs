using Microsoft.AspNetCore.Mvc;

namespace Undersoft.SDK.Service.Server.Controller.Abstractions;
public interface IRemoteServiceController<TStore, TService, TDto>
    where TService : class
    where TDto : class
    where TStore : IDataServiceStore
{
    IActionResult Access([FromBody] IDictionary<string, Arguments> args);
    IActionResult Action([FromBody] IDictionary<string, Arguments> args);
    IActionResult Setup([FromBody] IDictionary<string, Arguments> args);
}
