using System.ServiceModel;

namespace Undersoft.SDK.Service.Server.Controller.Abstractions;

using Undersoft.SDK.Proxies;
using Undersoft.SDK.Service.Data.Query;
using Undersoft.SDK.Service.Data.Response;

[ServiceContract]
public interface IStreamController<TDto> where TDto : class, IOrigin, IInnerProxy
{
    Task<ResultString> Count();
    IAsyncEnumerable<TDto> Get();   
    IAsyncEnumerable<ResultString> Post(TDto[] dtos);
    IAsyncEnumerable<ResultString> Patch(TDto[] dtos);
    IAsyncEnumerable<ResultString> Put(TDto[] dtos);
    IAsyncEnumerable<ResultString> Delete(TDto[] dtos);
}