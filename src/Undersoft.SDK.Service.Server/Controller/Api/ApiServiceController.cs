using Microsoft.AspNetCore.Mvc;

namespace Undersoft.SDK.Service.Server.Controller.Api;

using Microsoft.AspNetCore.Http;
using Undersoft.SDK;
using Undersoft.SDK.Service;
using Undersoft.SDK.Service.Data.Client.Attributes;
using Undersoft.SDK.Service.Data.Store;
using Undersoft.SDK.Service.Operation.Invocation;
using Undersoft.SDK.Service.Server.Controller.Api.Abstractions;
using Undersoft.SDK.Service.Server.Extensions;

[ApiService]
public abstract class ApiServiceController<TStore, TService, TModel>
    : ControllerBase,
        IApiServiceController<TStore, TService, TModel>
    where TModel : class, IOrigin, IInnerProxy
    where TService : class
    where TStore : IDataServerStore
{
    protected readonly IServicer _servicer;

    protected ApiServiceController() { }

    protected ApiServiceController(IServicer servicer)
    {
        _servicer = servicer.TryGetService<IHttpContextAccessor>(out var accessor)
            ? accessor.SetServicer(servicer)
            : servicer.SetServicer(servicer);
    }

    [HttpPost("Action/{method}")]
    public virtual async Task<IActionResult> Action(
        [FromRoute] string method,
        [FromBody] Arguments arguments
    )
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _servicer.Send(new Access<TStore, TService, TModel>(method, arguments));

        return !result.IsValid ? BadRequest(result.ErrorMessages) : Ok(result.Response);
    }

    [HttpPost("Access/{method}")]
    public virtual async Task<IActionResult> Access(
        [FromRoute] string method,
        [FromBody] Arguments arguments
    )
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _servicer.Send(new Action<TStore, TService, TModel>(method, arguments));

        return !result.IsValid ? BadRequest(result.ErrorMessages) : Ok(result.Response);
    }

    [HttpPost("Setup/{method}")]
    public virtual async Task<IActionResult> Setup(
        [FromRoute] string method,
        [FromBody] Arguments arguments
    )
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _servicer.Send(new Setup<TStore, TService, TModel>(method, arguments));

        return !result.IsValid ? BadRequest(result.ErrorMessages) : Ok(result.Response);
    }
}
