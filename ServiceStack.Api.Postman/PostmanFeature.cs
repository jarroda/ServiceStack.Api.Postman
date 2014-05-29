using ServiceStack.WebHost.Endpoints;
using ServiceStack.WebHost.Endpoints.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace ServiceStack.Api.Postman
{
    public sealed class PostmanFeature : IPlugin
    {
        public PostmanFeature()
        {
            DefaultLabel = "{type}";
        }

        public void Register(IAppHost appHost)
        {
            appHost.CatchAllHandlers.Add(ProcessRequest);
        }

        public bool LocalOnly { get; set; }

        public string DefaultLabel { get; set; }

        public IHttpHandler ProcessRequest(string httpMethod, string pathInfo, string filePath)
        {
            var pathParts = pathInfo.TrimStart('/').Split('/');
            return pathParts.Length == 0 ? null : GetHandlerForPathParts(pathParts);
        }

        private IHttpHandler GetHandlerForPathParts(string[] pathParts)
        {
            var pathController = string.Intern(pathParts[0].ToLower());
            if (pathController == "postman")
            {
                return new PostmanMetadataHandler { LocalOnly = LocalOnly };
            }
         
            return null;
        }
    }
}