namespace Undersoft.SDK.Service.Data.Client
{
    public partial class StreamClient<TStore> : DataClientStream where TStore : IDataServiceStore
    {
        public StreamClient(Uri serviceUri) : base(serviceUri) { } 
    }
}