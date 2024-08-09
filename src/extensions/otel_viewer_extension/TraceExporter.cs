using Grpc.Net.Client;
using OpenTelemetry.Proto.Collector.Trace.V1;
using System.Reflection;
using Google.Protobuf.WellKnownTypes; // For Empty and other common message types
using System;
using System.Threading.Tasks;
using Google.Cloud.Trace.V1;
using OtlpCollector = OpenTelemetry.Proto.Collector.Trace.V1;
using TraceService = OpenTelemetry.Proto.Collector.Trace.V1.TraceService;
using Azure.AI.Details.Common.CLI;

namespace Azure.AI.Details.Common.CLI.Extensions.Otel
{
    public class TraceExporter
    {
        
        private readonly OtlpCollector.TraceService.TraceServiceClient traceClient;

        public TraceExporter(string otlpEndpoint)
        {
            // Create a gRPC channel
            var channel = GrpcChannel.ForAddress(otlpEndpoint);

            traceClient = new TraceService.TraceServiceClient(channel);
        }

        internal async Task<ExportTraceServiceResponse> ExportSpansAsync(ExportTraceServiceRequest request)
        {
            var response = await traceClient.ExportAsync(request);

            if (response.PartialSuccess != null && response.PartialSuccess.RejectedSpans > 0)
            {
                Console.WriteLine($"Export partially succeeded: {response.PartialSuccess.RejectedSpans} spans rejected.");
                Console.WriteLine($"Error message: {response.PartialSuccess.ErrorMessage}");
            }
            else
            {
                Console.WriteLine("Export succeeded.");
            }
            return response;
        }
        

    }
}
