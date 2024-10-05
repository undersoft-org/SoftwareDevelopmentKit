namespace Undersoft.SDK.Service.Data.Store
{
    public static class StoreRoutes
    {
        public const string EntryStore = "entry";
        public const string ReportStore = "report";
        public const string EventStore = "";
        public const string DataStore = "data";
        public const string AccountStore = "auth";           
    }

    public class StoreRouteRegistry : Registry<(Type, string)>
    {
        public StoreRouteRegistry(IEnumerable<ISeriesItem<(Type, string)>> items) : base(items) { }

        public StoreRouteRegistry() : base() { }
    }
}
