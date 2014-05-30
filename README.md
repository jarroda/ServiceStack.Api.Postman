ServiceStack.Api.Postman [![Build status](https://ci.appveyor.com/api/projects/status/bwmnwyilk3eupiwd)](https://ci.appveyor.com/project/jarroda/servicestack-api-postman)
========================
[Postman](http://www.getpostman.com/) is a HTTP client designed to help test web services, and I have found it to be extremely helpful when developing REST based APIs.  One of its nicest features is the ability to save requests into '[collections](http://www.getpostman.com/docs/collections)', and the ability to import and export these collections for sharing with others.  I have written a simple ServiceStack plugin to enable the automatic generation of these collections.

## Installation

Install the NuGet package (https://www.nuget.org/packages/ServiceStack.Api.Postman/) or reference the library directly.

Enable Postman plugin in AppHost.cs with:

```csharp
    public override void Configure(Container container)
    {
        ...
        Plugins.Add(new PostmanFeature());
	
        // Add the CORS feature to allow direct import from the Postman app.
        Plugins.Add(new CorsFeature()); 
        ...
    }
```

Compile it. Now you can access the Postman endpoint at:
(Assuming service hosted at /api)

http://localhost:port/api/postman

Use this url when importing the collection into Postman.
## Configuration

By default, the postman service is only available when running locally.  To enable access from remote machines, turn off the LocalOnly flag.

```csharp
    public override void Configure(Container container)
    {
        ...
        Plugins.Add(new PostmanFeature { LocalOnly = false });
        ...
    }
```
#### Custom Labels
The generated label for the postman request can be customized using a format string with {type} and {route} placeholders.  These placeholders will be replaced with the request type and route and all other characters willl be left as-is.  The label format can be set either on the postman endpoint: `/postman?label={type}+{route}` or the `PostmanFeature`:

```csharp
    public override void Configure(Container container)
    {
        ...
        Plugins.Add(new PostmanFeature { DefaultLabel = "{type} {route}" });
        ...
    }
```
#### Custom Headers
Like custom labels, custom headers can be supplied for all postman requests.  By default, `Accept: text/json` is set on all requests, but this can be overridden on the postman endpoint or the `PostmanFeature`:

```csharp
    public override void Configure(Container container)
    {
        ...
        Plugins.Add(new PostmanFeature
        {
    	    DefaultHeaders = new[] { "Accept: text/json", "Authorization: Basic eDpwYXNzd29yZA==" },
        });
        ...
    }
```
or

    /postman?headers=Accept:+text/json,Authorization:+Basic+eDpwYXNzd29yZA==
