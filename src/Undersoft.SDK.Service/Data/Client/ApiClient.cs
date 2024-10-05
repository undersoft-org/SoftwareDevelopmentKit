namespace Undersoft.SDK.Service.Data.Client
{
    public partial class ApiClient<TStore> : ApiClientContext where TStore : IDataServiceStore
    {
        public ApiClient(Uri serviceUri) : base(serviceUri)
        {
        }
    }
}