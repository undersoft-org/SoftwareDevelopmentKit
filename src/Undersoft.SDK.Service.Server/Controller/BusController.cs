using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using System.Linq.Expressions;

namespace Undersoft.SDK.Service.Server.Controller;

using MediatR;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.AspNetCore.OData.Routing.Attributes;
using Operation.Command;
using Operation.Query;
using Undersoft.SDK.Proxies;
using Undersoft.SDK.Service.Data.Client.Attributes;
using Undersoft.SDK.Service.Data.Event;
using Undersoft.SDK.Service.Data.Store;
using Undersoft.SDK.Service.Server.Controller.Abstractions;

[Area("Bus")]
[DataOperator]
public abstract class BusController<TKey, TStore, TEntity, TDto, TService>
    : ServiceController<TStore, TService, TDto>,
        IEventController<TKey, TEntity, TDto>
    where TDto : class, IOrigin, IInnerProxy, new()
    where TEntity : class, IOrigin, IInnerProxy
    where TStore : IDataServerStore
    where TService : class
{
    protected Func<TKey, Func<TDto, object>> _keysetter = k => e => e.SetId(k);
    protected Func<TKey, Expression<Func<TEntity, bool>>> _keymatcher = k => e => k.Equals(e.Id);
    protected Func<TDto, Expression<Func<TEntity, bool>>> _predicate;
    protected IQueryParameters<TEntity> _parameters;
    protected readonly PublishMode _publishMode;

    protected BusController() { }

    public BusController(IServicer servicer)
       : base(servicer) { }

    protected BusController(
          IServicer servicer,
          PublishMode publishMode = PublishMode.Propagate,
          IQueryParameters<TEntity> parameters = null
      )
          : this(
              servicer,
              null,
              k => e => e.SetId(k),
              k => e => k.Equals(e.Id),
              publishMode,
              parameters
          )
    { }

    protected BusController(
        IServicer servicer,
        Func<TDto, Expression<Func<TEntity, bool>>> predicate,
        Func<TKey, Func<TDto, object>> keysetter,
        Func<TKey, Expression<Func<TEntity, bool>>> keymatcher,
        PublishMode publishMode = PublishMode.Propagate,
        IQueryParameters<TEntity> parameters = null
    ) : base(servicer)
    {       
        _keymatcher = keymatcher;
        _keysetter = keysetter;
        _publishMode = publishMode;
        _parameters = parameters;
    }

    [EnableQuery]
    public virtual IQueryable<TDto> Get()
    {
        return _servicer.Send(new Get<TStore, TEntity, TDto>(_parameters)).Result.Result;
    }

    [EnableQuery]
    public virtual SingleResult<TDto> Get([FromRoute] TKey key)
    {
        return new SingleResult<TDto>(
            _servicer
                .Send(
                    new Find<TStore, TEntity, TDto>(
                        new QueryParameters<TEntity>() { Filter = _keymatcher(key) }
                    )
                )
                .Result.Result
        );
    }

    [ODataIgnored]
    [HttpPost("[area]/[controller]({key})")]
    public virtual async Task<IActionResult> Post([FromRoute] TKey key, [FromBody] TDto dto)
    {
        if (!ModelState.IsValid)
            BadRequest(ModelState);

        _keysetter(key).Invoke(dto);

        return await Execute(new Create<TStore, TDto, TDto>(_publishMode, dto));
    }

    public virtual async Task<IActionResult> Patch([FromRoute] TKey key, [FromBody] TDto dto)
    {
        if (!ModelState.IsValid)
            BadRequest(ModelState);

        _keysetter(key).Invoke(dto);

        return await Execute(new Change<TStore, TDto, TDto>(_publishMode, dto, _predicate));
    }

    public virtual async Task<IActionResult> Put([FromRoute] TKey key, [FromBody] TDto dto)
    {
        if (!ModelState.IsValid)
            BadRequest(ModelState);

        _keysetter(key).Invoke(dto);

        return await Execute(new Update<TStore, TDto, TDto>(_publishMode, dto, _predicate));
    }

    [ODataIgnored]
    [HttpDelete("[area]/[controller]({key})")]
    public virtual async Task<IActionResult> Delete([FromRoute] TKey key, [FromBody] TDto dto)
    {
        if (!ModelState.IsValid)
            BadRequest(ModelState);

        _keysetter(key).Invoke(dto);

        return await Execute(new Delete<TStore, TDto, TDto>(_publishMode, dto));
    }

    [ODataIgnored]
    [HttpPost("[area]/[controller]")]
    public virtual async Task<IActionResult> Post([FromBody] TDto[] dtos)
    {
        if (!ModelState.IsValid)
            BadRequest(ModelState);

        return await ExecuteSet(new CreateSet<TStore, TDto, TDto>(_publishMode, dtos));
    }

    [ODataIgnored]
    [HttpPatch("[area]/[controller]")]
    public virtual async Task<IActionResult> Patch([FromBody] TDto[] dtos)
    {
        if (!ModelState.IsValid)
            BadRequest(ModelState);

        return await ExecuteSet(new ChangeSet<TStore, TDto, TDto>(_publishMode, dtos));
    }

    [ODataIgnored]
    [HttpPut("[area]/[controller]")]
    public virtual async Task<IActionResult> Put([FromBody] TDto[] dtos)
    {
        if (!ModelState.IsValid)
            BadRequest(ModelState);

        return await ExecuteSet(new UpdateSet<TStore, TDto, TDto>(_publishMode, dtos));
    }

    [ODataIgnored]
    [HttpDelete("[area]/[controller]")]
    public virtual async Task<IActionResult> Delete([FromBody] TDto[] dtos)
    {
        if (!ModelState.IsValid)
            BadRequest(ModelState);

        return await ExecuteSet(new DeleteSet<TStore, TDto, TDto>(_publishMode, dtos));
    }

    protected virtual async Task<IActionResult> ExecuteSet<TResult>(IRequest<TResult> request)
        where TResult : CommandSet<TDto>
    {
        var result = await _servicer.Send(request).ConfigureAwait(false);

        object[] response = result
            .ForEach(c => c.IsValid ? c.Id.ToString() : c.ErrorMessages)
            .ToArray();

        return result.Any(c => !c.IsValid) ? UnprocessableEntity(response) : Ok(response);
    }

    protected virtual async Task<IActionResult> Execute<TResult>(IRequest<TResult> request)
        where TResult : Command<TDto>
    {
        var result = await _servicer.Send(request).ConfigureAwait(false);

        return !result.IsValid
            ? UnprocessableEntity(result.ErrorMessages)
            : Ok(result.Id.ToString());
    }
}
