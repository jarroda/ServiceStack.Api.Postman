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

            var collection = new PostmanCollection
            {
                Id = collectionId,
                Name = EndpointHost.Config.ServiceName,
                Timestamp = DateTime.UtcNow.ToUnixTimeMs(),
                Requests = metadata.Operations.SelectMany(op =>
                {
                    op.ToString();

                    return op.Routes.Where(r => !r.AllowsAllVerbs).SelectMany(route => route.AllowedVerbs.Split(',').Select(verb =>
                    {
                        string name; 

                        switch(verb)
                        {
                            case "GET":
                                if(route.Path.Contains("{Id}"))
                                {
                                    name = "Get " + op.RequestType.Name + " by ID";
                                }
                                else
                                {
                                    name = "Get all " + op.RequestType.Name;
                                }
                                break;
                            case "DELETE":
                                if(route.Path.Contains("{Id}"))
                                {
                                    name = "Delete " + op.RequestType.Name + " by ID";
                                }
                                else
                                {
                                    name = "Delete all " + op.RequestType.Name;
                                }
                                break;
                            case "POST":
                                if(route.Path.Contains("{Id}"))
                                {
                                    name = "Update " + op.RequestType.Name + " by ID";
                                }
                                else
                                {
                                    name = "Create " + op.RequestType.Name;
                                }
                                break;
                            default:
                                name = verb.ToTitleCase() + " " + op.RequestType.Name;
                                break;
                        }

                        return new PostmanRequest
                        {
                            Id = Guid.NewGuid().ToString(),
                            CollectionId = collectionId,
                            Method = verb,
                            Name = name,
                            Url = httpReq.GetApplicationUrl() + route.Path,
                            Description = op.RequestType.GetDescription(),
                            Headers = "Accept: application/json",
                            Version = 2, // Magic number
                            DataMode = "params",
                            Time = DateTime.UtcNow.ToUnixTimeMs(),
                            Data = verb != "POST" ? null : ReflectionUtils.PopulateObject(op.RequestType.CreateInstance()).ToStringDictionary().Select(a => new PostmanData
                            {
                                Key = a.Key,
                                Value = a.Value,
                                Type = "text",
                            }).ToArray(),
                        };
                    }));                    
                }).ToArray(),
            };

            using (var scope = JsConfig.BeginScope())
            {
                
                scope.EmitCamelCaseNames = true;
                JsonSerializer.SerializeToStream(collection, responseStream);
            }
        }
    }
}