using System;
using System.ClientModel.Primitives;
using System.IO;
using System.Text;

namespace Azure.AI.Details.Common.CLI;

public class LogTrafficEventPolicy : TrafficEventPolicy
{
    public LogTrafficEventPolicy()
    {
        OnRequest += (sender, request) => LogRequest(request);
        OnResponse += (sender, response) => LogResponse(response);
    }

    private static void LogRequest(PipelineRequest request)
    {
        AI.TRACE_INFO($"===== REQUEST: {request.Method} {request.Uri}");

        // foreach ((string headerName, string headerValue) in request.Headers)
        // {
        //     // Could handle request headers here
        // }

        try
        {
            using MemoryStream dumpStream = new();
            request.Content?.WriteTo(dumpStream);
            dumpStream.Position = 0;
            BinaryData requestData = BinaryData.FromStream(dumpStream);

            var line = requestData.ToString().Replace("\n", "\\n").Replace("\r", "");
            if (!string.IsNullOrWhiteSpace(line))
            {
                AI.TRACE_INFO($"===== REQUEST BODY: {line}");
            }
        }
        catch
        {
        }
    }

    private static void LogResponse(PipelineResponse response)
    {
        AI.TRACE_INFO($"===== RESPONSE: {response.Status} ({response.ReasonPhrase})");

        var sb = new StringBuilder();
        foreach ((string headerName, string headerValue) in response.Headers)
        {
            sb.Append($"{headerName}: {headerValue}\n");
        }
        var headers = sb.ToString().Replace("\n", "\\n").Replace("\r", "");
        if (!string.IsNullOrWhiteSpace(headers))
        {
            AI.TRACE_INFO($"===== RESPONSE HEADERS: {headers}");
        }

        try
        {
            var line = response.Content?.ToString()?.Replace("\n", "\\n")?.Replace("\r", "");
            AI.TRACE_INFO($"===== RESPONSE BODY: {line}");
        }
        catch
        {
        }
    }}
