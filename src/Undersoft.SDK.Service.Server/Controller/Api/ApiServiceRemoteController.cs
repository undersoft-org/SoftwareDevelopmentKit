﻿using Microsoft.AspNetCore.Mvc;

namespace Undersoft.SDK.Service.Server.Controller.Api;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Undersoft.SDK;
using Undersoft.SDK.Service;
using Undersoft.SDK.Service.Access;
using Undersoft.SDK.Service.Data.Client.Attributes;
using Undersoft.SDK.Service.Data.Store;
using Undersoft.SDK.Service.Server.Controller.Api.Abstractions;
using Undersoft.SDK.Service.Server.Extensions;

[ApiController]
[ApiServiceRemote]
public abstract class ApiServiceRemoteController<TStore, TService, TModel>
    : ControllerBase,
        IApiServiceRemoteController<TStore, TService, TModel>
    where TModel : class, IOrigin, IInnerProxy
    where TService : class
    where TStore : IDataServiceStore
{
    protected readonly IServicer _servicer;

    protected ApiServiceRemoteController() { }

    protected ApiServiceRemoteController(IServicer servicer)
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

        var result = await _servicer.Send(
            new RemoteAccess<TStore, TService, TModel>(method, arguments)
        );

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

        var result = await _servicer.Send(
            new RemoteAction<TStore, TService, TModel>(method, arguments)
        );

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

        var result = await _servicer.Send(
            new RemoteSetup<TStore, TService, TModel>(method, arguments)
        );

        return !result.IsValid ? BadRequest(result.ErrorMessages) : Ok(result.Response);
    }
}
