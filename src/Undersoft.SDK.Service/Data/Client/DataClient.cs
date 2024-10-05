namespace Undersoft.SDK.Service.Data.Client
{
    public partial class DataClient<TStore> : DataClientContext where TStore : IDataServiceStore
    {
        public DataClient(Uri serviceUri) : base(serviceUri)
        {
        }
    }
}