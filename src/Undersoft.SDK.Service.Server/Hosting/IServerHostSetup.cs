namespace Undersoft.SDK.Service.Server.Hosting
{
    public interface IServerHostSetup
    {
        IServerHostSetup UseHeaderForwarding();

        IServerHostSetup UseServiceServer(bool useMultitenancy = true, string[] apiVersions = null);

        IServerHostSetup UseCustomSetup(Action<IServerHostSetup> action);

        IServerHostSetup UseServiceClients(int delayInSeconds = 0);

        IServerHostSetup UseServiceRouter();

        IServerHostSetup UseDefaultProvider();

        IServerHostSetup UseInternalProvider();

        IServerHostSetup UseDataMigrations();

        IServerHostSetup UseEndpoints(bool useRazorPages = false);

        IServerHostSetup UseJwtMiddleware();

        IServerHostSetup UseMultitenancy();

        IServerHostSetup RebuildProviders();

        IServerHostSetup UseSwaggerSetup(string[] apiVersions);

        IServerHostSetup MapFallbackToFile(string filePath);

        IServiceManager Manager { get; }
    }
}