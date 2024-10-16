using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace Undersoft.SDK.Service.Server.Controller.Open;

using Microsoft.AspNetCore.Http;
using System.Text.Json;
using Undersoft.SDK.Service;
using Undersoft.SDK.Service.Data.Client.Attributes;
using Undersoft.SDK.Service.Data.Store;
using Undersoft.SDK.Service.Operation.Invocation;
using Undersoft.SDK.Service.Server.Extensions;
using Undersoft.SDK.Service.Server.Controller.Open.Abstractions;

[OpenServiceRemote]
public abstract class OpenServiceRemoteController<TStore, TService, TDto>
    : ODataController,
        IOpenServiceRemoteController<TStore, TService, TDto>
    where TService : class
    where TDto : class
    where TStore : IDataServiceStore
{
    protected readonly IServicer _servicer;

    protected OpenServiceRemoteController() { }

    public OpenServiceRemoteController(IServicer servicer)
    {
        _servicer = servicer.TryGetService<IHttpContextAccessor>(out var accessor)
            ? accessor.SetServicer(servicer)
            : servicer.SetServicer(servicer);
    }

    [HttpPost]
    public virtual IActionResult Access([FromBody] IDictionary<string, Arguments> args)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = Invoke(
            args,
            (arg) => new RemoteAccess<TStore, TService, TDto>(arg.Key, arg.Value)
        );

        Task.WaitAll(result);

        var response = result.Select(r => r.Result).FirstOrDefault();
        var payload = response.ToJsonBytes();
        return !response.IsValid ? BadRequest(payload) : Ok(payload);
    }

    [HttpPost]
    public virtual IActionResult Action([FromBody] IDictionary<string, Arguments> args)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = Invoke(
            args,
            (arg) => new RemoteAction<TStore, TService, TDto>(arg.Key, arg.Value)
        );

        Task.WaitAll(result);

        var response = result.Select(r => r.Result).FirstOrDefault();
        var payload = response.ToJsonBytes();
        return !response.IsValid ? BadRequest(payload) : Ok(payload);
    }

    [HttpPost]
    public virtual IActionResult Setup([FromBody] IDictionary<string, Arguments> args)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = Invoke(
            args,
            (arg) => new RemoteSetup<TStore, TService, TDto>(arg.Key, arg.Value)
        );

        Task.WaitAll(result);

        var response = result.Select(r => r.Result).FirstOrDefault();
        var payload = response.ToJsonBytes();
        return !response.IsValid ? BadRequest(payload) : Ok(payload);
    }

    public virtual Task<Arguments>[] Invoke(
        IDictionary<string, Arguments> args,
        Func<KeyValuePair<string, Arguments>, Invocation<TDto>> invocation
    )
    {
        return args.ForEach(async a =>
            {
                var preresult = await _servicer.Send(invocation(a));
                return (Arguments)preresult.Output;
            })
            .Commit();
    }
}
