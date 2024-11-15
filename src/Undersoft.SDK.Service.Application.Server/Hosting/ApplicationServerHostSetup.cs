using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using Undersoft.SDK.Service.Server.Hosting;

namespace Undersoft.SDK.Service.Application.Server.Hosting;

public class ApplicationServerHostSetup : ServerHostSetup, IApplicationServerHostSetup
{
    public ApplicationServerHostSetup(IApplicationBuilder application) : base(application) { }

    public ApplicationServerHostSetup(
        IApplicationBuilder application,
        IWebHostEnvironment environment
    ) : base(application, environment) { }

    public IApplicationServerHostSetup UseServiceApplication(bool useMultitenancy = true, string[]? apiVersions = null)
    {
        UseHeaderForwarding();       

        if (_environment.IsDevelopment())
        {
            _builder.UseDeveloperExceptionPage()
                .UseWebAssemblyDebugging();
        }
        else
        {
            _builder.UseExceptionHandler("/Error")
                .UseHsts();
        }

        _builder
            .UseODataBatching()
            .UseODataQueryRequest()
            .UseBlazorFrameworkFiles()            
            #if NET8_0
            .UseStaticFiles()
            #else
            .MapStaticAssets()
            #endif
            .UseRouting()           
            .UseCors(o => o.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());

        if(apiVersions != null )
            UseSwaggerSetup(apiVersions);

        _builder.UseAuthentication()
            .UseAuthorization();

        if (useMultitenancy)
            UseMultitenancy();

        _builder.UseApplicationTracking();

        UseEndpoints(true);

        return this;
    }
}
