﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace Undersoft.SDK.Service.Server.Controller;
using System.Linq;
using System.Text.Json;
using Undersoft.SDK.Proxies;
using Undersoft.SDK.Service;
using Undersoft.SDK.Service.Data.Client.Attributes;
using Undersoft.SDK.Service.Data.Store;
using Undersoft.SDK.Service.Operation.Invocation;
using Undersoft.SDK.Service.Server.Controller.Abstractions;

[ServiceOperator]
public abstract class ServiceController<TStore, TService, TModel>
    : ODataController,
        IServiceController<TStore, TService, TModel> where TModel : class, IOrigin, IInnerProxy, new()
    where TService : class
    where TStore : IDataServerStore
{
    protected readonly IServicer _servicer;

    protected ServiceController() { }

    public ServiceController(IServicer servicer)
    {
        _servicer = servicer;
    }

    [HttpPost]
    public virtual IActionResult Access([FromBody] IDictionary<string, Arguments> args)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = Invoke(
            args,
            (arg) => new Access<TStore, TService, TModel>(arg.Key, arg.Value)
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
            (arg) => new Action<TStore, TService, TModel>(arg.Key, arg.Value)
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

        var result = Invoke(args, (arg) => new Setup<TStore, TService, TModel>(arg.Key, arg.Value));

        Task.WaitAll(result);

        var response = result.Select(r => r.Result).FirstOrDefault();
        var payload = response.ToJsonBytes();
        return !response.IsValid ? BadRequest(payload) : Ok(payload);
    }

    protected virtual Task<Arguments>[] Invoke(
        IDictionary<string, Arguments> args,
        Func<KeyValuePair<string, Arguments>, Invocation<TModel>> invocation
    )
    {
        return args.ForEach(async a =>
            {            
                var preresult = await _servicer.Send(invocation(a));        
                if (preresult.GetType().IsArray)
                    return new Arguments(
                        a.Key,
                        ((object[])preresult.Output).ForEach(o => new Argument(o, a.Key)
                        {
                            IsValid = preresult.IsValid
                        })
                    );
                return new Arguments(
                    a.Key,
                    new Argument(preresult.Output, a.Key) { IsValid = preresult.IsValid }
                );
            })
            .Commit();
    }
}
