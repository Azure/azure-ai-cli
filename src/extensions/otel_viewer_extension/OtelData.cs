using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using OpenTelemetry.Proto.Collector.Trace.V1;
using Newtonsoft.Json.Linq;
using Google.Protobuf;
using Azure.AI.Details.Common.CLI;

namespace Azure.AI.Details.Common.CLI.Extensions.Otel
{
    public class OtelData
    {
        public static async Task<string> GetTrace(string requestId)
        {
            // Endpoint URL with the request ID
            string url = $"https://spanretriever1-webapp.whitehill-dc9ec001.westus.azurecontainerapps.io/getspan/{requestId}";
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // Send a GET request to the endpoint
                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();

                    // Convert JSON to a formatted string
                    var parsedJson = JsonConvert.DeserializeObject(responseBody);
                    string formattedJson = JsonConvert.SerializeObject(parsedJson, Formatting.Indented);

                    return formattedJson;
                }
                catch (HttpRequestException httpEx)
                {
                    Console.WriteLine("HTTP Request failed: " + httpEx.Message);
                    return null;
                }
            }

        }
        public static async Task WriteDataToFile(string data, string filePath)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    await writer.WriteAsync(data);
                    Console.WriteLine($"Payload successfully written to {filePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while writing to the file: {ex.Message}");
            }

        }
        public static async Task ExportToDashboard(string data)
        {
            try
            {
                
                // Check if the dashboard (OTLP endpoint) is running
                if (!Dashboard.IsRunning())
                {
                    Console.WriteLine("Dashboard is not running. Please start the dashboard and try again.");
                    return; 
                }
                
                var settings = new JsonSerializerSettings();
                settings.Converters.Add(new AnyValueConverter());
                settings.Converters.Add(new IntArrayToByteStringConverter());

                var request = JsonConvert.DeserializeObject<ExportTraceServiceRequest>(data, settings);
        
                var otlpEndpoint = "http://localhost:4317";
                var otlpExporter = new TraceExporter(otlpEndpoint);
                var exportResponse = await otlpExporter.ExportSpansAsync(request);

                Console.WriteLine("Span data processed and sent to exporter.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred during export: " + ex.Message);
            }
        }
    }
}
