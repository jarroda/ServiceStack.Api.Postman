using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Routing;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.OrmLite;
using System.IO;
using ServiceStack.Api.Postman;
using ServiceStack.ServiceInterface.Cors;

namespace FluentMigrator.ServiceStack.TestV3
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801
    public class MvcApplication : System.Web.HttpApplication
    {
        public class AppHost : AppHostBase
        {
            public AppHost() : base("Test Web Services", typeof(AppHost).Assembly) { }

            public override void Configure(Funq.Container container)
            {
                Plugins.Add(new CorsFeature());
                Plugins.Add(new PostmanFeature());
            }
        }

        protected void Application_Start()
        {
            new AppHost().Init();
        }
    }
}