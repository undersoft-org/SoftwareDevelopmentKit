using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;

namespace Undersoft.SDK.Service.Server.Controller;

using Microsoft.AspNetCore.OData.Results;
using Microsoft.AspNetCore.OData.Routing.Attributes;
using Operation.Command;
using Operation.Query;
using Undersoft.SDK.Proxies;
using Undersoft.SDK.Service.Data.Client.Attributes;
using Undersoft.SDK.Service.Data.Event;
using Undersoft.SDK.Service.Data.Store;
using Undersoft.SDK.Service.Server.Controller.Attributes;

[Area("Data")]
[DataOperator]
[RemoteResult]
public abstract class CqrsController<TKey, TEntry, TReport, TEntity, TDto, TService>
    : DataController<TKey, TEntry, TEntity, TDto, TService>
    where TDto : class, IOrigin, IInnerProxy, new()
    where TEntity : class, IOrigin, IInnerProxy
    where TEntry : IDataServerStore
    where TReport : IDataServerStore
    where TService : class
{
    protected CqrsController() { }

    public CqrsController(IServicer servicer)
        : base(servicer) { }

    protected CqrsController(
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

    protected CqrsController(
        IServicer servicer,
        Func<TDto, Expression<Func<TEntity, bool>>> predicate,
        Func<TKey, Func<TDto, object>> keysetter,
        Func<TKey, Expression<Func<TEntity, bool>>> keymatcher,
        PublishMode publishMode = PublishMode.Propagate,
        IQueryParameters<TEntity> parameters = null
    )
        : base(servicer)
    {
        _keymatcher = keymatcher;
        _keysetter = keysetter;
        _publishMode = publishMode;
        _parameters = parameters;
    }

    [EnableQuery]
    public override IQueryable<TDto> Get()
    {
        return _servicer.Send(new Get<TReport, TEntity, TDto>(_parameters)).Result.Result;
    }

    [EnableQuery]
    public override SingleResult<TDto> Get([FromRoute] TKey key)
    {
        return new SingleResult<TDto>(
            _servicer
                .Send(
                    new Find<TReport, TEntity, TDto>(
                        new QueryParameters<TEntity>() { Filter = _keymatcher(key) }
                    )
                )
                .Result.Result
        );
    }

    [ODataIgnored]
    [HttpPost("[area]({key})")]
    public override async Task<IActionResult> Post([FromRoute] TKey key, [FromBody] TDto dto)
    {
        if (!ModelState.IsValid)
            BadRequest(ModelState);

        _keysetter(key).Invoke(dto);

        return await Execute(new Create<TEntry, TEntity, TDto>(_publishMode, dto));
    }

    public override async Task<IActionResult> Patch([FromRoute] TKey key, [FromBody] TDto dto)
    {
        if (!ModelState.IsValid)
            BadRequest(ModelState);

        _keysetter(key).Invoke(dto);

        return await Execute(new Change<TEntry, TEntity, TDto>(_publishMode, dto, _predicate));
    }

    public override async Task<IActionResult> Put([FromRoute] TKey key, [FromBody] TDto dto)
    {
        if (!ModelState.IsValid)
            BadRequest(ModelState);

        _keysetter(key).Invoke(dto);

        return await Execute(new Update<TEntry, TEntity, TDto>(_publishMode, dto, _predicate));
    }

    [ODataIgnored]
    [HttpDelete("[area]({key})")]
    public override async Task<IActionResult> Delete([FromRoute] TKey key, [FromBody] TDto dto)
    {
        if (!ModelState.IsValid)
            BadRequest(ModelState);

        _keysetter(key).Invoke(dto);

        return await Execute(new Delete<TEntry, TEntity, TDto>(_publishMode, dto));
    }

    [ODataIgnored]
    [HttpPut("[area]")]
    public override async Task<IActionResult> Put([FromBody] TDto[] dtos)
    {
        if (!ModelState.IsValid)
            BadRequest(ModelState);

        return await ExecuteSet(
            new UpdateSet<TEntry, TEntity, TDto>(_publishMode, dtos, _predicate)
        );
    }

    [ODataIgnored]
    [HttpPatch("[area]")]
    public override async Task<IActionResult> Patch([FromBody] TDto[] dtos)
    {
        if (!ModelState.IsValid)
            BadRequest(ModelState);

        return await ExecuteSet(
            new ChangeSet<TEntry, TEntity, TDto>(_publishMode, dtos, _predicate)
        );
    }

    [ODataIgnored]
    [HttpPost("[area]")]
    public override async Task<IActionResult> Post([FromBody] TDto[] dtos)
    {
        if (!ModelState.IsValid)
            BadRequest(ModelState);

        return await ExecuteSet(new CreateSet<TEntry, TEntity, TDto>(_publishMode, dtos));
    }

    [ODataIgnored]
    [HttpDelete("[area]")]
    public override async Task<IActionResult> Delete([FromBody] TDto[] dtos)
    {
        if (!ModelState.IsValid)
            BadRequest(ModelState);

        return await ExecuteSet(new DeleteSet<TEntry, TEntity, TDto>(_publishMode, dtos));
    }   
}
