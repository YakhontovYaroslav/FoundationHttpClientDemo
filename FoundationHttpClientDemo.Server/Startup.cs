using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Cors;
using Owin;

namespace FoundationHttpClientDemo.Server
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.Use<AuthTokenHeaderAuthentication>();
            app.UseErrorPage();
            app.UseCors(CorsOptions.AllowAll);

            var hubConfiguration = new HubConfiguration
                                       {
                                           EnableDetailedErrors = true,
                                           EnableJavaScriptProxies = false
                                       };
            app.MapSignalR(hubConfiguration);
        }
    }
}
