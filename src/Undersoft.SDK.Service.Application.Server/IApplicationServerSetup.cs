namespace Undersoft.SDK.Service.Application.Server;

using Undersoft.SDK.Service.Server;

public partial interface IApplicationServerSetup : IServerSetup
{
    IServerSetup ConfigureApplicationServer(
        bool includeSwagger = false,
        Type[]? sourceTypes = null,
        Type[]? clientTypes = null
    );
}
