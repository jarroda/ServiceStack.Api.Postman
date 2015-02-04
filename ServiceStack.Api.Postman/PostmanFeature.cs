using ServiceStack.Common.Web;
using ServiceStack.WebHost.Endpoints;
using System.Web;

namespace ServiceStack.Api.Postman
{
    public sealed class PostmanFeature : IPlugin
    {
        public PostmanFeature()
        {
            DefaultLabel = "{type}";
            DefaultHeaders = new[] { "Accept: " + MimeTypes.Json };
            SupportWebApplication = true;
            SupportFolders = true;
            DoNotAllowFolderIfOnlyOneItem = true;
        }

        public void Register(IAppHost appHost)
        {
            appHost.CatchAllHandlers.Add(ProcessRequest);
        }

        public bool LocalOnly { get; set; }

        /// <summary>
        /// Indicates if the PostmanFeature should support WebApplications (defaults to true).
        /// i.e. when set to false The URLS generated for http://host/app1/api/request will be
        /// http://host/api/request when set to true they will be http://host/app1/api/request 
        /// </summary>
        public bool SupportWebApplication { get; set; }

        /// <summary>
        /// Indicates if the PostmanFeature should support Folders (defaults to true).
        /// i.e. when set to false. I will put all generated requests in the root of the collection.
        /// When true it will put them in folders if appropiate.  http://host/app1/api/folder1/request
        /// Will be put in folder1 in the collection.
        /// </summary>
        public bool SupportFolders { get; set; }

        /// <summary>
        /// If true all folders with one item will have their output in the root of the collection instead.
        /// Default = true. SupportFolders has to be true for this setting to have effect
        /// </summary>
        public bool DoNotAllowFolderIfOnlyOneItem { get; set; }

        public string DefaultLabel { get; set; }

        public string[] DefaultHeaders { get; set; }

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
                return new PostmanMetadataHandler { LocalOnly = LocalOnly, SupportFolders = SupportFolders, SupportWebApplication = SupportWebApplication, DoNotAllowFolderIfOnlyOneItem = true };
            }
         
            return null;
        }
    }
}