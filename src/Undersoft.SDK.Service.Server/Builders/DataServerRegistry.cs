namespace Undersoft.SDK.Service.Server.Builders
{
    public static class DataServerRegistry
    {
        public static ISeries<ISeries<Type>> StreamControllers = new Registry<ISeries<Type>>();

        public static ISeries<ISeries<Type>> DataControllers = new Registry<ISeries<Type>>();

        public static ISeries<ISeries<IInvoker>> DataInvokers = new Registry<ISeries<IInvoker>>();
    }   
}
