﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace Undersoft.SDK.Service.Server.Controller;

using System.Text.Json;
using Undersoft.SDK.Service;
using Undersoft.SDK.Service.Data.Client.Attributes;
using Undersoft.SDK.Service.Data.Store;
using Undersoft.SDK.Service.Operation.Invocation;
using Undersoft.SDK.Service.Server.Controller.Abstractions;

[RemoteServiceOperator]
public abstract class RemoteServiceController<TStore, TService, TDto>
    : ODataController,
        IRemoteServiceController<TStore, TService, TDto>
    where TService : class
    where TDto : class
    where TStore : IDataServiceStore
{
    protected readonly IServicer _servicer;

    protected RemoteServiceController() { }

    public RemoteServiceController(IServicer servicer)
    {
        _servicer = servicer;
    }

    [HttpPost]
    public virtual IActionResult Service([FromBody] IDictionary<string, Arguments> args)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = Invoke(
            args,
            (arg) => new RemoteService<TStore, TService, TDto>(arg.Key, arg.Value)
        );

        Task.WaitAll(result);

        var response = result.Select(r => r.Result).FirstOrDefault();
        var payload = response.ToJsonBytes();
        return !response.IsValid ? BadRequest(payload) : Ok(payload);
    }   

    protected virtual Task<Arguments>[] Invoke(
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
