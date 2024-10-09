using System;
using System.ClientModel.Primitives;
using System.IO;
using System.Text;

namespace Azure.AI.Details.Common.CLI;

public class GetRequestIdLogTrafficEventPolicy : TrafficEventPolicy
{
    public GetRequestIdLogTrafficEventPolicy(string filepath, bool outputId, bool addId)
    {
        OnRequest += (sender, request) => LogRequest(request);
        OnResponse += (sender, response) => LogResponse(response, filepath, outputId, addId);
    }

    private static void LogRequest(PipelineRequest request)
    {
    }

    private static void LogResponse(PipelineResponse response, string filepath, bool outputId, bool addId)
    {
        string apimRequestId = null;
        foreach ((string headerName, string headerValue) in response.Headers)
        {
            if (headerName.Equals("apim-request-id", StringComparison.OrdinalIgnoreCase))
            {
                apimRequestId = headerValue;
                break;
            }
        }
         if (!string.IsNullOrWhiteSpace(apimRequestId))
        {
            // Determine the write mode: append or overwrite
            FileMode mode = addId ? FileMode.Append : FileMode.Create;

            try
            {
                using (FileStream fs = new FileStream(filepath, mode))
                using (StreamWriter writer = new StreamWriter(fs))
                {
                    writer.WriteLine(apimRequestId);
                }
            }
            catch (Exception ex)
            {
                AI.TRACE_ERROR($"Failed to write Request ID to file: {ex.Message}");
            }
        }
        else
        {
            AI.TRACE_WARNING("APIM Request ID not found in response headers.");
        }
    }
}
