﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Formatter;

namespace Undersoft.SDK.Service.Server.Controller.Abstractions;

using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Results;

public interface IRemoteDataController<TKey, TDto, TModel> where TModel : class, IOrigin, IInnerProxy
    where TDto : class, IOrigin, IInnerProxy
{
    IQueryable<TModel> Get(ODataQueryOptions<TModel> options);
    SingleResult<TModel> Get([FromODataUri] TKey key, ODataQueryOptions<TModel> options);

    Task<IActionResult> Patch([FromBody] TModel[] models);
    Task<IActionResult> Patch([FromRoute] TKey key, [FromBody] TModel model);

    Task<IActionResult> Post([FromBody] TModel[] models);
    Task<IActionResult> Post([FromRoute] TKey key, [FromBody] TModel model);

    Task<IActionResult> Put([FromBody] TModel[] models);
    Task<IActionResult> Put([FromRoute] TKey key, [FromBody] TModel model);

    Task<IActionResult> Delete([FromBody] TModel[] models);
    Task<IActionResult> Delete([FromRoute] TKey key, [FromBody] TModel model);
}