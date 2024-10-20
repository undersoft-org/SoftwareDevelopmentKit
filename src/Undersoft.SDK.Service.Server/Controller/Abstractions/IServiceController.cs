using Microsoft.AspNetCore.Mvc;

namespace Undersoft.SDK.Service.Server.Controller.Abstractions;
public interface IServiceController<TStore, TService, TModel> where TModel : class, IOrigin, IInnerProxy, new()
    where TService : class
    where TStore : IDataServerStore
{
    IActionResult Access([FromBody] IDictionary<string, Arguments> args);
    IActionResult Action([FromBody] IDictionary<string, Arguments> args);
    IActionResult Setup([FromBody] IDictionary<string, Arguments> args);
}