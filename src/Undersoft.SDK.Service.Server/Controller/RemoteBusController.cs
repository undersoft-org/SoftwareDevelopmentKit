using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using System.Linq.Expressions;

namespace Undersoft.SDK.Service.Server.Controller;

using Data.Event;
using MediatR;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.AspNetCore.OData.Routing.Attributes;
using Operation.Remote.Command;
using Operation.Remote.Query;
using Undersoft.SDK.Proxies;
using Undersoft.SDK.Service.Data.Client.Attributes;
using Undersoft.SDK.Service.Data.Store;
using Undersoft.SDK.Service.Server.Controller.Abstractions;

[Area("Bus")]
[RemoteDataOperator]
public abstract class RemoteBusController<TKey, TStore, TDto, TModel, TService>
    : RemoteServiceController<TStore, TService, TDto>,
        IRemoteEventController<TKey, TModel>
    where TModel : class, IOrigin, IInnerProxy
    where TDto : class, IOrigin, IInnerProxy
    where TStore : IDataServiceStore
    where TService : class
{
    protected Func<TKey, Func<TModel, object>> _keysetter = k => e => e.SetId(k);
    protected Func<TKey, Expression<Func<TDto, bool>>> _keymatcher = k => e => k.Equals(e.Id);
    protected Func<TModel, Expression<Func<TDto, bool>>> _predicate;
    protected readonly PublishMode _publishMode;
    protected ODataQuerySettings _settings;

    protected RemoteBusController() { }

    protected RemoteBusController(
        IServicer servicer,
        PublishMode publishMode = PublishMode.Propagate
    )
        : this(servicer, null, k => e => e.SetId(k), k => e => k.Equals(e.Id), publishMode) { }

    protected RemoteBusController(
        IServicer servicer,
        Func<TModel, Expression<Func<TDto, bool>>> predicate,
        PublishMode publishMode = PublishMode.Propagate
    )
        : this(servicer, predicate, k => e => e.SetId(k), k => e => k.Equals(e.Id), publishMode) { }

    protected RemoteBusController(
        IServicer servicer,
        Func<TModel, Expression<Func<TDto, bool>>> predicate,
        Func<TKey, Func<TModel, object>> keysetter,
        Func<TKey, Expression<Func<TDto, bool>>> keymatcher,
        PublishMode publishMode = PublishMode.Propagate
    )
    {
        _keymatcher = keymatcher;
        _keysetter = keysetter;
        _publishMode = publishMode;
        _settings = new ODataQuerySettings()
        {
            HandleNullPropagation = HandleNullPropagationOption.False,
            HandleReferenceNavigationPropertyExpandFilter = true,
            IgnoredQueryOptions = AllowedQueryOptions.Expand,
        };
    }

    public virtual IQueryable<TModel> Get(ODataQueryOptions<TModel> options)
    {
        var query = _servicer.Send(new RemoteGet<TStore, TDto, TModel>()).Result.Result;
        var result = options.ApplyTo(query, _settings);
        return (IQueryable<TModel>)result;
    }

    public virtual SingleResult<TModel> Get([FromRoute] TKey key, ODataQueryOptions<TModel> options)
    {
        var query =
            _servicer.Send(
                new RemoteFind<TStore, TDto, TModel>(
                    new QueryParameters<TDto>() { Filter = _keymatcher(key) }
                )
            )

            .Result
            .Result;
        var result = options.ApplyTo(query, _settings);
        return new SingleResult<TModel>((IQueryable<TModel>)result);
    }

    [ODataIgnored]
    [HttpPost("[area]({key})")]
    public virtual async Task<IActionResult> Post([FromRoute] TKey key, [FromBody] TModel dto)
    {
        if (!ModelState.IsValid)
            BadRequest(ModelState);

        _keysetter(key).Invoke(dto);

        return await Execute(new RemoteCreate<TStore, TDto, TModel>(_publishMode, dto));
    }

    public virtual async Task<IActionResult> Patch([FromRoute] TKey key, [FromBody] TModel dto)
    {
        if (!ModelState.IsValid)
            BadRequest(ModelState);

        _keysetter(key).Invoke(dto);

        return await Execute(new RemoteChange<TStore, TDto, TModel>(_publishMode, dto, _predicate));
    }

    public virtual async Task<IActionResult> Put([FromRoute] TKey key, [FromBody] TModel dto)
    {
        if (!ModelState.IsValid)
            BadRequest(ModelState);

        _keysetter(key).Invoke(dto);

        return await Execute(new RemoteUpdate<TStore, TDto, TModel>(_publishMode, dto, _predicate));
    }

    [ODataIgnored]
    [HttpDelete("[area]({key})")]
    public virtual async Task<IActionResult> Delete([FromRoute] TKey key, [FromBody] TModel dto)
    {
        if (!ModelState.IsValid)
            BadRequest(ModelState);

        _keysetter(key).Invoke(dto);

        return await Execute(new RemoteDelete<TStore, TDto, TModel>(_publishMode, dto));
    }

    [ODataIgnored]
    [HttpPost("[area]")]
    public virtual async Task<IActionResult> Post([FromBody] TModel[] dtos)
    {
        if (!ModelState.IsValid)
            BadRequest(ModelState);

        return await ExecuteSet(new RemoteCreateSet<TStore, TDto, TModel>(_publishMode, dtos));
    }

    [ODataIgnored]
    [HttpPatch("[area]")]
    public virtual async Task<IActionResult> Patch([FromBody] TModel[] dtos)
    {
        if (!ModelState.IsValid)
            BadRequest(ModelState);

        return await ExecuteSet(
            new RemoteChangeSet<TStore, TDto, TModel>(_publishMode, dtos, _predicate)
        );
    }

    [ODataIgnored]
    [HttpPut("[area]")]
    public virtual async Task<IActionResult> Put([FromBody] TModel[] dtos)
    {
        if (!ModelState.IsValid)
            BadRequest(ModelState);

        return await ExecuteSet(
            new RemoteUpdateSet<TStore, TDto, TModel>(_publishMode, dtos, _predicate)
        );
    }

    [ODataIgnored]
    [HttpDelete("[area]")]
    public virtual async Task<IActionResult> Delete([FromBody] TModel[] dtos)
    {
        if (!ModelState.IsValid)
            BadRequest(ModelState);

        return await ExecuteSet(new RemoteDeleteSet<TStore, TDto, TModel>(_publishMode, dtos));
    }

    protected virtual async Task<IActionResult> ExecuteSet<TResult>(IRequest<TResult> request)
        where TResult : RemoteCommandSet<TModel>
    {
        var result = await _servicer.Send(request).ConfigureAwait(false);

        object[] response = result
            .ForEach(c => c.IsValid ? c.Id as object : c.ErrorMessages)
            .ToArray();

        return result.Any(c => !c.IsValid) ? UnprocessableEntity(response) : Ok(response);
    }

    protected virtual async Task<IActionResult> Execute<TResult>(IRequest<TResult> request)
        where TResult : RemoteCommand<TModel>
    {
        var result = await _servicer.Send(request).ConfigureAwait(false);

        return !result.IsValid ? UnprocessableEntity(result.ErrorMessages) : Ok(result.Id);
    }
}
