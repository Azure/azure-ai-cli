You

i want to build a dotnet service that acts as a "proxy" of a kind, not a real proxy, mind you.

I'm using a javascript SDK from OpenAI, and it needs a couple headers. Instead of adding them in the javascript on the client side, I want to add them in a server side "proxy/relay" whose job is to listen on all "routes" under "relay". For each route request that comes in, the service component will add a couple headers (they're constant, so just use placeholders).

important items:

I have no control over the paths/routes. Only, that I can insert a new "node" at the root of the route ... I want to have everything under "relay/..." go to "/..." on a new endpoint.
I want the exact same headers, and the exact same method, and the exact same query parameters, etc... The only difference is that it'll add 1-2 headers.
it must support "streaming" responses from the backend... for example, there's a route currently called "/runs/threads/foobar/blah/run?api_version=xxx&" that gets is a POST method, and content type is "text/event-stream; charset=utf-8" (e.g. SSE events); i don't want to introduce any noticable delay in the responses
Assistant

Sure, to build this service in .NET, you could actually build a HTTP Middleware that captures and processes your incoming requests, adds necessary headers, then forwards the request to its destination. Here is a simple example:

public class MyProxyRelayMiddleware
{
    private readonly RequestDelegate _next;

    public MyProxyRelayMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var originalRequest = context.Request;

        var newRequest = new HttpRequestMessage();
        newRequest.RequestUri = new Uri("https://mynewserviceurl.com" + originalRequest.Path); // replace with the endpoint you would proxy to
        newRequest.Method = new HttpMethod(originalRequest.Method);

        foreach (var header in originalRequest.Headers)
        {
            newRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }
        //Add your custom headers
        newRequest.Headers.TryAddWithoutValidation("MyHeaderKey", "MyHeaderValue");

        if (originalRequest.Method != "GET" && originalRequest.Body != null) 
        {
            using (var streamReader = new StreamReader(originalRequest.Body))
            {
                var requestBody = await streamReader.ReadToEndAsync();
                newRequest.Content = new StringContent(requestBody);
            }
        }
        
        using (var httpClient = new HttpClient())
        {
            var response = await httpClient.SendAsync(newRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            foreach (var header in response.Headers)
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }

            context.Response.Headers.Remove("transfer-encoding");

            await context.Response.WriteAsync(responseContent);
        }
    }
}

This middleware intercepts the original request, creates a new one, copying all headers from the original along with adding your custom headers, sends the new request, then returns the response.

Don't forget to add your middleware in the Configure method of Startup.cs file.

public void Configure(IApplicationBuilder app)
{
    app.UseMiddleware<MyProxyRelayMiddleware>();
    ...
}

As for streaming responses, there might be additional work involved. The HTTPClient.SendAsync method by default buffers the entire response in memory. If you set HttpCompletionOption to ResponseHeadersRead, then SendAsync returns as soon as the headers have been read which should allow for streaming scenarios.

NOTE: The code is provided as is, and might require additional refinements / adjustments to fit with the actual complexity of your application like exception handling, authentications, request/response transformations, etc.

You

that's not going to stream properly. try again.

Assistant

To set up a streaming reverse proxy, where the data arrives in piecemeal, passed on immediately to the client, as opposed to waiting for the full response before passing on to client, you can use the SendAsync method with the HttpCompletionOption.ResponseHeadersRead to ensure immediate reading of the response headers.

Here is an example of a Middleware in ASP.NET Core that could serve your purpose:

public class ProxyMiddleware
{
    private static readonly HttpClient _httpClient = new HttpClient();
    private readonly RequestDelegate _nextMiddleware;

    public ProxyMiddleware(RequestDelegate nextMiddleware)
    {
        _nextMiddleware = nextMiddleware;
    }

    public async Task Invoke(HttpContext context)
    {
        var targetUri = BuildTargetUri(context.Request);

        using (var requestMessage = CreateForwardMessage(context, targetUri))
        {
            using (var responseMessage = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted))
            {
                context.Response.StatusCode = (int)responseMessage.StatusCode;
                CopyFromTargetResponseHeaders(context, responseMessage);
                await ProcessResponseContent(context, responseMessage);
            }
        }
    }

    private HttpRequestMessage CreateForwardMessage(HttpContext context, Uri targetUri)
    {
        var requestMessage = new HttpRequestMessage();
        CopyFromOriginalRequestContentAndHeaders(context, requestMessage);

        requestMessage.RequestUri = targetUri;
        requestMessage.Headers.Host = targetUri.Host;
        requestMessage.Method = GetMethod(context.Request.Method);
        // Add your proxy specific headers here
        requestMessage.Headers.Add("X-Proxy", "MyProxy");

        return requestMessage;
    }

    private async Task ProcessResponseContent(HttpContext context, HttpResponseMessage responseMessage)
    {
        var buffer = new byte[1024];

        using (var input = await responseMessage.Content.ReadAsStreamAsync())
        using (var output = context.Response.Body)
        {
            int bytesRead;
            do
            {
                bytesRead = await input.ReadAsync(buffer, 0, buffer.Length);

                if (bytesRead == 0)
                {
                    break;
                }

                await output.WriteAsync(buffer, 0, bytesRead);
            } while (true);
        }
    }
    // Other private methods like BuildTargetUri, CopyFromOriginalRequestContentAndHeaders, GetMethod, CopyFromTargetResponseHeaders
}

This Middleware can be registered in your Startup.cs file.

public void Configure(IApplicationBuilder app)
{
    app.UseMiddleware<ProxyMiddleware>();
    ...
}

In this code, GetMethod is a function to convert string to HttpMethod, CopyFromOriginalRequestContentAndHeaders is a method to transfer headers from the original request to the new one and BuildTargetUri constructs the new URI for the request from the original, replacing the host with the target one. CopyFromTargetResponseHeaders copies headers from the original response to the new one.

Please be aware that you may need to carefully consider what parts of the original request can be safely included in the forwarded request without leaking sensitive data and what headers are necessary on forward responses.

Also, if you are processing requests that contain 'Authorization' or similar headers they should be removed before making the forwarding call (HttpClient sends 'Authorization' header only if the request is to the same Uri as the original by default for security reasons).

You

when reading the response data from the backend isn't there a way to get the "chunks" as they come in? let's imagine we optimized this for SSE scenarios. Meaning, it only needs to stream those.

Assistant

Yes, you can handle this by using HttpCompletionOption.ResponseHeadersRead in HttpClient.SendAsync method which returns the control to you as soon as it reads response headers. Afterwards, you can start streaming reading the response without buffering it first:

public class ProxyMiddleware
{
    private static readonly HttpClient _httpClient = new HttpClient();
    private readonly RequestDelegate _nextMiddleware;
    private const string SSE_CONTENT_TYPE = "text/event-stream; charset=utf-8";

    public ProxyMiddleware(RequestDelegate nextMiddleware)
    {
        _nextMiddleware = nextMiddleware;
    }

    public async Task Invoke(HttpContext context)
    {
        // Set up headers
        var requestMessage = new HttpRequestMessage();
        var targetUri = BuildTargetUri(context.Request);
        requestMessage.RequestUri = targetUri;
        requestMessage.Method = GetMethod(context.Request.Method);
        requestMessage.Headers.Add("X-Proxy", "MyProxy");
        CopyFromOriginalRequestContentAndHeaders(context, requestMessage);

        using (var responseMessage = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted))
        {
            if (responseMessage.Content.Headers.ContentType?.MediaType == SSE_CONTENT_TYPE)
            {
                ProcessSseResponse(context, responseMessage);
            }
            else
            {
                context.Response.StatusCode = (int)responseMessage.StatusCode;
                CopyFromTargetResponseHeaders(context, responseMessage);
                await ProcessResponseContent(context, responseMessage);
            }
        }
    }

    private async Task ProcessSseResponse(HttpContext context, HttpResponseMessage responseMessage)
    {
        context.Response.ContentType = SSE_CONTENT_TYPE;
        context.Response.StatusCode = (int)HttpStatusCode.OK;

        var buffer = new byte[4096];

        using (var output = context.Response.Body)
        {
            var stream = await responseMessage.Content.ReadAsStreamAsync();

            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await output.WriteAsync(buffer, 0, bytesRead);
                await output.FlushAsync(); // this can be removed in .NET Core 3+
            }
        }
    }
}

The ProcessSseResponse method checks if the ContentType of the incoming response from your target server is Server-Sent Events, if true then it streams the content in chunks as it is received. The FlushAsync method outputs the data to the response as it arrives, rather than buffering until the entire response is ready. This would enable server-sent events to be relayed through your proxy as they're sent, without being buffered. Note that in .NET Core 3.0 and later, the FlushAsync is called implicitly after a WriteAsync, so you wouldn't need to call FlushAsync every time you write to the stream.

You

Make ProcessSseResponse read one line at a time, where lines are "\n" terminated... and once it reads a line, it will sent it to output.

Assistant

When dealing with Server-Sent Events (SSE), reading and writing one line at a time is crucial since the new lines indicate a new event, and these events need to be forwarded immediately. Here is how you can modify the ProcessSseResponse method to handle line by line stream:

private async Task ProcessSseResponse(HttpContext context, HttpResponseMessage responseMessage)
{
    context.Response.ContentType = SSE_CONTENT_TYPE;
    context.Response.StatusCode = (int)HttpStatusCode.OK;

    using (var output = context.Response.Body)
    {
        var stream = await responseMessage.Content.ReadAsStreamAsync();
        using (var reader = new StreamReader(stream))
        {
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                var bytes = Encoding.UTF8.GetBytes(line + '\n');
                await output.WriteAsync(bytes, 0, bytes.Length);
                await output.FlushAsync();
            }
        }
    }
}

In this modification, the StreamReader reads the stream line by line. After reading each line, it is immediately written to the output stream and flushed to make sure it’s sent immediately. The response line is converted back to bytes with UTF-8 encoding because WriteAsync requires bytes to write and SSE uses UTF-8 encoding. The '\n' is added when writing the line because ReadLineAsync does not include the newline character.

You

based on this last response, summarize into a "completely standalone" email I can send to a friend on how to perform this task.

Assistant

Subject: Building a Server-Side Event Proxy Service In .NET

Hi,

If you want to build a proxy service in .NET that forwards requests to another server and immediately sends back responses as they come, the key is to use ASP.NET Core Middleware and manipulate these responses on a stream basis. The HttpClient from .Net has a method HttpClient.SendAsync(HttpRequestMessage, HttpCompletionOption, CancellationToken), and HttpCompletionOption.ResponseHeadersRead returns the control as soon as it reads response headers, then you can start reading the response. Especially effective for Server-Sent Events (SSE), where reading and writing one line at a time is crucial.

Here is an example:

public class ProxyMiddleware
{
    private static readonly HttpClient _httpClient = new HttpClient();
    private readonly RequestDelegate _nextMiddleware;
    private const string SSE_CONTENT_TYPE = "text/event-stream; charset=utf-8";

    public ProxyMiddleware(RequestDelegate nextMiddleware)
    {
        _nextMiddleware = nextMiddleware;
    }

    public async Task Invoke(HttpContext context)
    {
        var targetUri = BuildTargetUri(context.Request);

        var requestMessage = new HttpRequestMessage();
        requestMessage.RequestUri = targetUri;
        requestMessage.Method = GetMethod(context.Request.Method);
        requestMessage.Headers.Add("X-Proxy", "MyProxy");
        CopyFromOriginalRequestContentAndHeaders(context, requestMessage);

        using (var responseMessage = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted))
        {
            if (responseMessage.Content.Headers.ContentType?.MediaType == SSE_CONTENT_TYPE)
            {
                await ProcessSseResponse(context, responseMessage);
            }
            else
            {
                context.Response.StatusCode = (int)responseMessage.StatusCode;
                CopyFromTargetResponseHeaders(context, responseMessage);
                await ProcessResponseContent(context, responseMessage);
            }
        }
    }

    private async Task ProcessSseResponse(HttpContext context, HttpResponseMessage responseMessage)
    {
        context.Response.ContentType = SSE_CONTENT_TYPE;
        context.Response.StatusCode = (int)HttpStatusCode.OK;

        using (var output = context.Response.Body)
        {
            var stream = await responseMessage.Content.ReadAsStreamAsync();
            using (var reader = new StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    var bytes = Encoding.UTF8.GetBytes(line + '\n');
                    await output.WriteAsync(bytes, 0, bytes.Length);
                    await output.FlushAsync(); 
                }
            }
        }
    }

    // Other private methods like BuildTargetUri, CopyFromOriginalRequestContentAndHeaders, GetMethod, CopyFromTargetResponseHeaders
}

And in your Startup.cs:

public void Configure(IApplicationBuilder app)
{
    app.UseMiddleware<ProxyMiddleware>();
}

This script, when added as a middleware in ASP.NET, intercepts incoming requests and forwards them to a specified target URI. If the content type of the response from the target URI is a Server-Sent Event, it processes the response line by line, immediately forwarding the data as it arrives without buffering.

Please note that the example is a basic implementation and doesn't take some important considerations into account like authentication, error handling, etc. which may be necessary based on the context of your project.

Anonymous methods like BuildTargetUri, CopyFromOriginalRequestContentAndHeaders, GetMethod, and CopyFromTargetResponseHeaders need to be implemented as well. These methods handle constructing the target URI, copying both content and headers from the original request to the new request, converting the string method to HttpMethod, and copying headers from the target response to the proxy response respectively.

I hope this gives a good roadmap on how you can achieve your requirement of a .NET proxy service.

Best, [Your Name]

You

scratch that... let's make into a blog article that starts from an empty directory, and builds everything up bit by bit... start with "dotnet new" to create the app, and start from the initial framing of a dotnet service/app.

Assistant

Building a Server-Side Event Proxy Service In .NET From Scratch
Dear Reader,

Today, we are going to talk about how to build a .NET server-side event proxy from scratch all the way from an empty directory to a fully working service. This approach allows us to handle Streaming Server-Sent Events (SSE) from a remote server, read and write responses immediately as they come.

Let's dive in.

Setting Up
Firstly, make sure to have .NET Core installed on your system. If you don't, you can download it from .NET Core's official page.

Start by creating a new project with the following command:

dotnet new webapi -n SseProxyApp

That'll create a new directory SseProxyApp and generate a new ASP.NET Core API project.

Navigate to the new directory:

cd SseProxyApp

Crafting the Middleware
The core of this proxy service is a Middleware that will take the incoming requests, forward them, and then stream the response back to the client.

In /Middleware/ProxyMiddleware.cs, let's start writing our Middleware.

using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

public class ProxyMiddleware
{
    private static readonly HttpClient _httpClient = new HttpClient();
    private readonly RequestDelegate _nextMiddleware;
    private const string SSE_CONTENT_TYPE = "text/event-stream; charset=utf-8";

    public ProxyMiddleware(RequestDelegate nextMiddleware)
    {
        _nextMiddleware = nextMiddleware;
    }

    // Remaining code will go here
}

Here, we are defining a private static HttpClient that will be used to forward requests to the remote server. A RequestDelegate reference _nextMiddleware is also leveraged to call the next delegate/middleware in the request pipeline.

Now we create the Invoke method which we will fill in parts with four main steps:

1- Creating a new HttpRequestMessage which will be forwarded

2- Sending the request to the remote server

3- Linking the incoming response type (SSE or non-SSE)

4- Processing the response content.

public async Task Invoke(HttpContext context)
{
    // 1- Creating the HttpRequestMessage

    // 2- Sending the request and getting the response

    // 3- Checking the type of the response

    // 4- Processing the response content
}

Let's flesh these out:

1- Creating the HttpRequestMessage:

var targetUri = new Uri("http://remote.server.com"); // Replace with the URL of your remote server
var requestMessage = new HttpRequestMessage();

requestMessage.RequestUri = targetUri;
requestMessage.Method = new HttpMethod(context.Request.Method);
context.Request.Headers
    .Where(h => !string.Equals(h.Key, "Host", StringComparison.InvariantCultureIgnoreCase))
    .ToList()
    .ForEach(header => requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()));

// Add your custom headers here
// requestMessage.Headers.Add("Custom-Header", "Value");

2- Let's send the request and get the response:

using (var responseMessage = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted))
{
   // 3- Checking the type of the response and Processing the response content will go here
}

3 & 4- Now, we will decide on how to process the response, by checking whether it's SSE or not:

using (var responseMessage = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted))
{
      if (responseMessage.Content.Headers.ContentType?.MediaType == SSE_CONTENT_TYPE)
      {
           await ProcessSseResponse(context, responseMessage);
      }
      else
      {
           context.Response.StatusCode = (int)responseMessage.StatusCode;
           foreach (var header in responseMessage.Headers)
           {
               context.Response.Headers[header.Key] = header.Value.ToArray();
           }
           await ProcessResponseContent(context, responseMessage);
      }
}

As for the ProcessSseResponse and ProcessResponseContent methods:

private async Task ProcessSseResponse(HttpContext context, HttpResponseMessage responseMessage)
{
    context.Response.ContentType = SSE_CONTENT_TYPE;
    context.Response.StatusCode = (int)HttpStatusCode.OK;

    using (var output = context.Response.Body)
    {
        var stream = await responseMessage.Content.ReadAsStreamAsync();
        using (var reader = new StreamReader(stream))
        {
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                var bytes = Encoding.UTF8.GetBytes(line + '\n');
                await output.WriteAsync(bytes, 0, bytes.Length);
                await output.FlushAsync(); 
            }
        }
    }
}

private async Task ProcessResponseContent(HttpContext context, HttpResponseMessage responseMessage)
{
    await responseMessage.Content.CopyToAsync(context.Response.Body);
}

Registering the Middleware
Now let's register the Middleware. Open up the Startup.cs file, and put app.UseMiddleware<ProxyMiddleware>(); into the Configure method:

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    // ...

    app.UseMiddleware<ProxyMiddleware>();

    // ...
}

That's it! Now you have a .NET server-side event proxy that takes incoming requests, forwards them to a specific server, and then streams back server-sent events as they come, line by line, without any buffering.

Please note that this serves as a basic implementation and does not handle important aspects such as exception handling and validating and modifying response headers, which may be necessary based on the specifics of your project.

-Thinking Machines

You

what if we also wanted to include some static web pages into that "app" ... how?

Assistant

Absolutely, including static pages in your application is quite straightforward. The key is to use the built-in Static Files middleware in ASP.NET Core which allows your app to serve static files such as HTML, CSS and JavaScript.

Follow the steps below to easily add static files to your application:

Add a folder named wwwroot in the root of the project, if it doesn't already exist.

Place your static files like HTML, CSS, and JavaScript in this wwwroot folder. For instance, you could create an index.html file here.

Enable the static files middleware in your Startup.cs file. To do this, add the UseStaticFiles() middleware in the Configure method. It should look something like this:

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    // ...

    app.UseStaticFiles();
    
    app.UseMiddleware<ProxyMiddleware>();

    // ...
}

Ensure that you call the UseStaticFiles method before the custom Proxy middleware. This helps when a request comes in: the application first checks if the requested resource corresponds to a static file, and if it does, it will serve that file. If not, it'll continue down the pipeline to your custom middleware.

Once these steps are completed, you should be able to navigate to any static files you put in the wwwroot directory on your server. For instance, if you have an index.html in wwwroot and your server is running at http://localhost:5000/, you would navigate to http://localhost:5000/index.html to access that file.

For more complex structures, you can further organize your static files by adding subdirectories inside the wwwroot folder. For instance, you can create a css folder for CSS files, a js folder for JavaScript files, etc. The static files middleware respects the folder structure inside wwwroot.

In addition, you might want to modify the UseStaticFiles() call to add a default document, which the server will serve if the client doesn't request a specific page. Use this code after app.UseStaticFiles():

app.UseDefaultFiles();

This code will make your application respond with the index.html file when a user visits the root URL. If you want a different file to serve as your default, you can include its name as a parameter in the UseDefaultFiles method.

Remember that the order of middleware matters, and UseDefaultFiles must be called before UseStaticFiles to serve the default file.

app.UseDefaultFiles();
app.UseStaticFiles();

You

will this, in essence, send all requests that are not static files directly to the target uri?

i'd like to have some "settings" to control for what "routes" that happens and for which they don't... for example, maybe the target uri should only handle routes that start with "/openai"... can we make that configurable somehow?

Assistant

If you want to limit the proxy functionality to certain routes, you could utilize ASP.NET Core's routing features in combination with MapWhen function which predicates the middleware based on the request.

In the following example, the proxy middleware is only invoked for the requests with a path that starts with "/openai":

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    // ...

    app.UseStaticFiles();

    app.MapWhen(
        context => context.Request.Path.StartsWithSegments("/openai"), 
        appBranch => { appBranch.UseMiddleware<ProxyMiddleware>(); }
    );

    // ...
}

With the above code in place, only the requests that have paths starting with "/openai" will be handled by the ProxyMiddleware.

To make it more flexible and configurable, you can store the routes in the appsettings.json file like:

{
  "ProxySettings": {
    "Routes": ["/openai", "/another_route"]
  }
}

And then inject the configuration into the Startup class and use it to conditionally apply the middleware:

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    // ...

    app.UseStaticFiles();

    var proxyRoutes = Configuration.GetSection("ProxySettings:Routes").Get<string[]>();
    foreach (var route in proxyRoutes)
    {
        app.MapWhen(
            context => context.Request.Path.StartsWithSegments(route), 
            appBranch => { appBranch.UseMiddleware<ProxyMiddleware>(); }
        );
    }

    // ...
}

This approach with MapWhen allows you to have a fine-grained control over when the proxy middleware should be triggered. This way, you can have different middlewares being executed based on various conditions from the incoming requests making your app more flexible and modular.

