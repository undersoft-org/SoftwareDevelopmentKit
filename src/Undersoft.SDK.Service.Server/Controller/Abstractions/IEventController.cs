using Microsoft.AspNetCore.Mvc;

namespace Undersoft.SDK.Service.Server.Controller.Abstractions;

using Microsoft.AspNetCore.OData.Results;
using Undersoft.SDK.Proxies;

public interface IEventController<TKey, TEntity, TDto> where TDto : class, IOrigin, IInnerProxy
{
    IQueryable<TDto> Get();

    SingleResult<TDto> Get([FromBody] TKey key);

    Task<IActionResult> Patch([FromBody] TDto[] models);
    Task<IActionResult> Patch([FromRoute] TKey key, [FromBody] TDto model);

    Task<IActionResult> Post([FromBody] TDto[] models);
    Task<IActionResult> Post([FromRoute] TKey key, [FromBody] TDto model);

    Task<IActionResult> Put([FromBody] TDto[] models);
    Task<IActionResult> Put([FromRoute] TKey key, [FromBody] TDto model);

    Task<IActionResult> Delete([FromBody] TDto[] models);
    Task<IActionResult> Delete([FromRoute] TKey key, [FromBody] TDto model);
}