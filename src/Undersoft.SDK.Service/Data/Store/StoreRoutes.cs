namespace Undersoft.SDK.Service.Data.Store
{
    public static class StoreRoutes
    {
        public const string EntryStore = "Entry";
        public const string ReportStore = "Report";
        public const string EventStore = "";
        public const string DataStore = "Data";
        public const string AccountStore = "Auth";           
    }

    public class StoreRouteRegistry : Registry<(Type, string)>
    {
        public StoreRouteRegistry(IEnumerable<ISeriesItem<(Type, string)>> items) : base(items) { }

        public StoreRouteRegistry() : base() { }
    }
}
