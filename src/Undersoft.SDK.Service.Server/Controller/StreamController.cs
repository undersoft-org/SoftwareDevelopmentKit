using System.Linq.Expressions;
using System.Text.Json;

namespace Undersoft.SDK.Service.Server.Controller;

using Microsoft.AspNetCore.Mvc;
using Operation.Command;
using Operation.Query;
using Undersoft.SDK.Proxies;
using Undersoft.SDK.Service.Data.Client.Attributes;
using Undersoft.SDK.Service.Data.Event;
using Undersoft.SDK.Service.Data.Query;
using Undersoft.SDK.Service.Data.Response;
using Undersoft.SDK.Service.Data.Store;
using Undersoft.SDK.Service.Server.Controller.Abstractions;

[StreamData]
public abstract class StreamController<TKey, TEntry, TReport, TEntity, TDto> : ControllerBase, IStreamController<TDto>
    where TDto : class, IOrigin, IInnerProxy
    where TEntity : class, IOrigin, IInnerProxy
    where TEntry : IDataServerStore
    where TReport : IDataServerStore
{
    protected Func<TKey, Func<TDto, object>> _keysetter = k => e => e.SetId(k);
    protected Func<TKey, Expression<Func<TEntity, bool>>> _keymatcher;
    protected Func<TDto, Expression<Func<TEntity, bool>>> _predicate;
    protected readonly IServicer _servicer;
    protected readonly PublishMode _publishMode;

    public StreamController() : this(new Servicer(), null, k => e => e.SetId(k), null, PublishMode.Propagate) { }

    public StreamController(IServicer servicer) : this(servicer, null, k => e => e.SetId(k), null, PublishMode.Propagate) { }

    public StreamController(IServicer servicer,
        Func<TDto, Expression<Func<TEntity, bool>>> predicate,
        Func<TKey, Func<TDto, object>> keysetter,
        Func<TKey, Expression<Func<TEntity, bool>>> keymatcher,
        PublishMode publishMode = PublishMode.Propagate
    )
    {
        _keymatcher = keymatcher;
        _keysetter = keysetter;
        _servicer = servicer;
        _publishMode = publishMode;
    }

    public virtual IAsyncEnumerable<TDto> Get()
    {
        return _servicer.CreateStream(new GetAsync<TReport, TEntity, TDto>(0, 0));
    }

    async Task<ResultString> IStreamController<TDto>.Count()
    {
        return await Task.FromResult(new ResultString(_servicer.StoreSet<TReport, TEntity>().Count().ToString()));
    }   

    public virtual IAsyncEnumerable<ResultString> Post(TDto[] dtos)
    {
        var result = _servicer.CreateStream(new CreateSetAsync<TEntry, TEntity, TDto>
                                                    (_publishMode, dtos));

        var response = result.ForEachAsync(c => new ResultString(c.IsValid
                                             ? c.Id.ToString()
                                             : c.ErrorMessages));
        return response;
    }

    public virtual IAsyncEnumerable<ResultString> Patch(TDto[] dtos)
    {
        var result = _servicer.CreateStream(new ChangeSetAsync<TEntry, TEntity, TDto>
                                                   (_publishMode, dtos));

        var response = result.ForEachAsync(c => new ResultString(c.IsValid
                                              ? c.Id.ToString()
                                              : c.ErrorMessages));
        return response;
    }

    public virtual IAsyncEnumerable<ResultString> Put(TDto[] dtos)
    {
        var result = _servicer.CreateStream(new UpdateSetAsync<TEntry, TEntity, TDto>
                                                 (_publishMode, dtos));

        var response = result.ForEachAsync(c => new ResultString(c.IsValid
                                              ? c.Id.ToString()
                                              : c.ErrorMessages));
        return response;
    }

    public virtual IAsyncEnumerable<ResultString> Delete(TDto[] dtos)
    {
        var result = _servicer.CreateStream(new DeleteSetAsync<TEntry, TEntity, TDto>
                                                  (_publishMode, dtos));

        var response = result.ForEachAsync(c => new ResultString(c.IsValid
                                             ? c.Id.ToString()
                                             : c.ErrorMessages));
        return response;
    }
}
