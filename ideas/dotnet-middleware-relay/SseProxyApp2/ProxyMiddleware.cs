using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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

    public async Task Invoke(HttpContext context)
    {
        // make the target uri the same as the incoming URI, just with a different host: https:///
        var targetUri = new UriBuilder(context.Request.Scheme, "robch-tipp-city2-oai.openai.azure.com")
        {
            Path = context.Request.Path,
            Query = context.Request.QueryString.ToString()
        }.Uri;
        var requestMessage = new HttpRequestMessage();

        requestMessage.RequestUri = targetUri;
        requestMessage.Method = new HttpMethod(context.Request.Method);
        context.Request.Headers
            .Where(h => !string.Equals(h.Key, "Host", StringComparison.InvariantCultureIgnoreCase))
            .ToList()
            .ForEach(header => requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()));

        // Add your custom headers here
        // requestMessage.Headers.Add("Custom-Header", "Value");

        if (context.Request.Method != "GET" && context.Request.Body != null) 
        {
            using (var streamReader = new StreamReader(context.Request.Body))
            {
                var requestBody = await streamReader.ReadToEndAsync();
                requestMessage.Content = new StringContent(requestBody);
            }
        }

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

    private async Task ProcessResponseContent(HttpContext context, HttpResponseMessage responseMessage)
    {
        await responseMessage.Content.CopyToAsync(context.Response.Body);
    }
}