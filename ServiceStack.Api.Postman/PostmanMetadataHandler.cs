using ServiceStack.Api.Postman.Types;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.WebHost.Endpoints.Support;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.Common.Utils;
using System.Net;
using ServiceStack.ServiceClient.Web;

namespace ServiceStack.Api.Postman
{
    public class PostmanMetadataHandler : HttpHandlerBase, IServiceStackHttpHandler
    {
        public bool LocalOnly { get; set; }
        public bool SupportWebApplication { get; set; }
        public bool SupportFolders { get; set; }
        public bool DoNotAllowFolderIfOnlyOneItem { get; set; }

        private const string PostmanSubPath = "/postman";
        private string _aspnetSubPath;

        public override void Execute(HttpContext context)
        {
            if (!LocalOnly || context.Request.IsLocal)
            {
                context.Response.ContentType = "application/json";

                _aspnetSubPath = CalculateAspnetSubRoute(context.Request);
                ProcessOperations(context.Response.OutputStream, new HttpRequestWrapper(GetType().Name, context.Request));
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                context.Response.End();
            }
        }

        private string CalculateAspnetSubRoute(HttpRequest request)
        {
            string url = request.Url.ToString();
            if (url.ToLowerInvariant().Contains(PostmanSubPath))
            {
                return url.Substring(0, url.LastIndexOf(PostmanSubPath, StringComparison.OrdinalIgnoreCase));
            }
            return url;
        }

        public void ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, string operationName)
        {
            if (!LocalOnly || httpReq.IsLocal)
            {
                httpRes.ContentType = "application/json";
                ProcessOperations(httpRes.OutputStream, httpReq);
            }
            else
            {
                httpRes.StatusCode = (int)HttpStatusCode.NotFound;
                httpRes.End();
            }
        }

        protected virtual void ProcessOperations(Stream responseStream, IHttpRequest httpReq)
        {
            var metadata = EndpointHost.Metadata;            

            var collectionId = Guid.NewGuid().ToString();

            var req =
                GetRequests(httpReq, metadata, collectionId, metadata.Operations)
                    .OrderBy(r => r.Folder)
                    .ThenBy(r => r.Name);
            
            var collection = new PostmanCollection
            {
                Id = collectionId,
                Name = EndpointHost.Config.ServiceName,
                Timestamp = DateTime.UtcNow.ToUnixTimeMs(),
                Requests = req.ToArray(),
                Order = new List<string>(),
                Folders = new List<PostmanFolder>()
            };

            AddToFolders(collection);
            if (SupportFolders && DoNotAllowFolderIfOnlyOneItem)
            {
                MoveOneItemFoldersOneLevelUp(collection);
            }
            
            using (var scope = JsConfig.BeginScope())
            {
                scope.EmitCamelCaseNames = true;
                JsonSerializer.SerializeToStream(collection, responseStream);
            }
        }

        private void MoveOneItemFoldersOneLevelUp(PostmanCollection collection)
        {
            for (int folderIdx = collection.Folders.Count - 1; folderIdx >= 0; folderIdx--)
                //Counting backwards to be able to remove folders
            {
                var folder = collection.Folders[folderIdx];
                if (folder.Order.Count == 1)
                {
                    collection.Order.Add(folder.Order[0]);
                    collection.Folders.RemoveAt(folderIdx);
                }
            }
        }

        private void AddToFolders(PostmanCollection collection)
        {
            if(!collection.Requests.Any())
                return;

            foreach (var request in collection.Requests)
            {
                if (!SupportFolders || string.IsNullOrEmpty(request.Folder))
                {
                    collection.Order.Add(request.Id);
                }
                else
                {
                    var folder = collection.Folders.FirstOrDefault(f => f.Name.Equals(request.Folder));
                    if (folder == null)
                    {
                        folder = new PostmanFolder
                        {
                            CollectionId = collection.Id,
                            CollectionName = collection.Name,
                            Id = Guid.NewGuid().ToString(),
                            Name = request.Folder,
                            Order = new List<string> {request.Id}
                        };
                        collection.Folders.Add(folder);
                    }
                    else
                    {
                        folder.Order.Add(request.Id);
                    }
                }
            }
        }

        private IEnumerable<PostmanRequest> GetRequests(IHttpRequest request, ServiceMetadata metadata, string parentId, IEnumerable<Operation> operations)
        {
            var feature = EndpointHost.GetPlugin<PostmanFeature>();
            var label = request.GetParam("label") ?? feature.DefaultLabel;

            var customHeaders = request.GetParam("headers");
            var headers = customHeaders == null ? feature.DefaultHeaders : customHeaders.Split(',');

            foreach (var op in metadata.OperationsMap.Values.Where(o => metadata.IsVisible(request, o)))
            {
                var exampleObject = ReflectionUtils.PopulateObject(op.RequestType.CreateInstance()).ToStringDictionary();
                
                var data = op.RequestType.GetSerializableFields().Select(f => f.Name)
                    .Concat(op.RequestType.GetSerializableProperties().Select(p => p.Name))
                    .ToDictionary(f => f, f => exampleObject.GetValueOrDefault(f));

                foreach (var route in op.Routes)
                {
                    var routeVerbs = route.AllowsAllVerbs ? new[] { "POST" } : route.AllowedVerbs.Split(new [] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                    var restRoute = new RestRoute(route.RequestType, route.Path, route.AllowedVerbs);

                    foreach (var verb in routeVerbs)
                    {
                        yield return new PostmanRequest
                        {                            
                            Id = Guid.NewGuid().ToString(),
                            Headers = string.Join("\n", headers),
                            Method = verb,
                            Url = CalculateAppUrl(request, _aspnetSubPath) + restRoute.Path.ReplaceVariables(),
                            Name = label.FormatLabel(op.RequestType, restRoute.Path),
                            Description = op.RequestType.GetDescription(),
                            PathVariables = restRoute.Variables.ToDictionary(v => v, v => data.GetValueOrDefault(v)),
                            Data = data.Keys.Except(restRoute.Variables).Select(v => new PostmanData
                            {
                                Key = v,
                                Value = data[v],
                                Type = "text",
                            }).ToArray(),
                            DataMode = "params",                            
                            Version = 2,
                            Time = DateTime.UtcNow.ToUnixTimeMs(),
                            CollectionId = parentId,
                            Folder = restRoute.Path.GetFolderName()
                        };
                    }
                }
            }
        }

        private string CalculateAppUrl(IHttpRequest request, string aspnetSubPath)
        {
            string serviceStackUrl = request.GetApplicationUrl();
            if (!SupportWebApplication)
                return serviceStackUrl; //Like version 1.0.4

            if (serviceStackUrl.Equals(aspnetSubPath))
                return serviceStackUrl;
            if (serviceStackUrl.StartsWith(aspnetSubPath))
                return serviceStackUrl;

            string newUrl;
            if (TryBuildNewUrl(serviceStackUrl, aspnetSubPath, out newUrl))
                return newUrl;

            return serviceStackUrl;
        }

        private bool TryBuildNewUrl(string serviceStackUrl, string aspnetSubPath, out string newUrl)
        {
            newUrl = string.Empty;
            if (!serviceStackUrl.Contains("/") || !aspnetSubPath.Contains("/"))
                return false;
            int lastSlash = serviceStackUrl.LastIndexOf("/", StringComparison.InvariantCultureIgnoreCase);
            string urlStart = serviceStackUrl.Substring(0, lastSlash);
            string urlEnd = serviceStackUrl.Substring(lastSlash);
            if(!aspnetSubPath.StartsWith(urlStart, StringComparison.InvariantCultureIgnoreCase))
                return false;

            string urlMiddle = aspnetSubPath.Substring(urlStart.Length);
            if (urlMiddle.EndsWith(urlEnd))
            {
                urlMiddle = urlMiddle.Substring(0, urlMiddle.Length - urlEnd.Length);
            }
            newUrl = urlStart + urlMiddle + urlEnd;
            return true;
        }
    }
}