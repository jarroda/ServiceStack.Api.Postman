using ServiceStack.Api.Postman.Types;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.WebHost.Endpoints.Support;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.Common.Utils;
using System.Net;
using ServiceStack.Common.Web;
using ServiceStack.ServiceClient.Web;

namespace ServiceStack.Api.Postman
{
    public class PostmanMetadataHandler : HttpHandlerBase, IServiceStackHttpHandler
    {
        public bool LocalOnly { get; set; }

        public override void Execute(HttpContext context)
        {
            if (!LocalOnly || context.Request.IsLocal)
            {
                context.Response.ContentType = "application/json";

                ProcessOperations(context.Response.OutputStream, new HttpRequestWrapper(GetType().Name, context.Request));
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                context.Response.End();
            }
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

            var req = GetRequests(httpReq, metadata, collectionId, metadata.Operations);
            
            var collection = new PostmanCollection
            {
                Id = collectionId,
                Name = EndpointHost.Config.ServiceName,
                Timestamp = DateTime.UtcNow.ToUnixTimeMs(),
                Requests = req.ToArray(),
            };
            
            using (var scope = JsConfig.BeginScope())
            {
                scope.EmitCamelCaseNames = true;
                JsonSerializer.SerializeToStream(collection, responseStream);
            }
        }

        private IEnumerable<PostmanRequest> GetRequests(IHttpRequest request, ServiceMetadata metadata, string parentId, IEnumerable<Operation> operations)
        {
            var feature = EndpointHost.GetPlugin<PostmanFeature>();
            var ret = new List<PostmanRequest>();            
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
                            Url = request.GetApplicationUrl() + restRoute.Path.ReplaceVariables(),
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
                        };
                    }
                }
            }
        }
    }
}