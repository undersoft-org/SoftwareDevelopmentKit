using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Formatter;

namespace Undersoft.SDK.Service.Server.Controller.Abstractions;

using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Results;
using Undersoft.SDK.Proxies;

public interface IDataController<TKey, TEntity, TDto>
    where TDto : class, IOrigin, IInnerProxy
    where TEntity : class, IOrigin, IInnerProxy
{
    [EnableQuery]
    IQueryable<TDto> Get();

    [EnableQuery]
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
