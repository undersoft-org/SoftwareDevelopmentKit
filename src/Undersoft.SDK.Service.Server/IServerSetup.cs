using Microsoft.EntityFrameworkCore;

namespace Undersoft.SDK.Service.Server
{
    using Undersoft.SDK.Service.Access;
    using Undersoft.SDK.Service.Data.Store;
    using Undersoft.SDK.Service.Server.Builders;

    public partial interface IServerSetup : IServiceSetup
    {
        IServerSetup AddDataServer<TServiceStore>(
            DataServiceTypes dataServiceTypes = DataServiceTypes.All,
            Action<DataServerBuilder> builder = null
        ) where TServiceStore : IDataStore;
        IServerSetup AddAccessServer<TContext, TAccount>() where TContext : DbContext where TAccount : class, IOrigin, IAuthorization;
        IServiceSetup AddRepositorySources(Type[] sourceTypes);
        IServiceSetup AddRepositorySources();
        IServerSetup AddAuthentication();
        IServerSetup AddAuthorization();
        IServerSetup UseServiceClients();
        IServerSetup AddApiVersions(string[] apiVersions);
        IServerSetup ConfigureServer(bool includeSwagger = false, Type[] sourceTypes = null, Type[] clientTypes = null);
    }
}
