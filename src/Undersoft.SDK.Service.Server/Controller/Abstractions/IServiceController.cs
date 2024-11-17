using Microsoft.AspNetCore.Mvc;

namespace Undersoft.SDK.Service.Server.Controller.Abstractions;
public interface IServiceController<TStore, TService, TModel> where TModel : class, IOrigin, IInnerProxy, new()
    where TService : class
    where TStore : IDataServerStore
{
    IActionResult Service([FromBody] IDictionary<string, Arguments> args);   
}