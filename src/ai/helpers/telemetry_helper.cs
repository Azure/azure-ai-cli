using System;
using System.Text;
using System.Text.Json;
using Azure.AI.Details.Common.CLI.Telemetry;

#if RELEASE
    #pragma warning disable CS0168 // Variable is declared but never used
#endif

namespace Azure.AI.Details.Common.CLI
{
    class TelemetryHelpers
    {
        private static readonly JsonSerializerOptions JSON_OPTIONS = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        internal static ITelemetry Instantiate(TelemetryConfig config, IProgramData programData)
        {
            try
            {
                switch (config.Type)
                {
                    case "none":
                    default:
                        return NoOpTelemetry.Instance;
                    case "aria":
                        return new AriaTelemetry(config.Aria.TenantId, programData);
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                ErrorHelpers.WriteLineMessage(
                    "WARNING:", "Failed to instantiate telemetry", ex.Message);
#endif
                return NoOpTelemetry.Instance;
            }
        }

        internal static ITelemetry InstantiateFromConfig(IProgramData programData, string telemetryConfig = "telemetry.config.json", INamedValues values = null)
        {
            // ignore failures
            TryParseConfigFile(telemetryConfig, values, out TelemetryConfig config);

            return Instantiate(config, programData);
        }

        internal static bool TryParseConfigFile(string fileName, INamedValues values, out TelemetryConfig config)
        {
            config = new TelemetryConfig();

            try
            {
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    return false;
                }

                /*
                 * NOTE:
                 * =====================
                 * The checked in telemetry.config.json is only used while debugging. As part of the CI/CD pipeline
                 * this is modified to use a different configuration using the FileTransform Azure DevOps task. This
                 * works by substituing JSON values using variables in JPath format. See here for more information:
                 * https://learn.microsoft.com/azure/devops/pipelines/tasks/reference/file-transform-v2
                 */
                string path = FileHelpers.FindFileInConfigPath(fileName, values);
                if (string.IsNullOrWhiteSpace(path))
                {
                    return false;
                }

                string jsonConfig = FileHelpers.ReadAllText(path, Encoding.UTF8);
                var wrapper = JsonSerializer.Deserialize<TelemetryConfigWrapper>(jsonConfig, JSON_OPTIONS);
                config = wrapper.Telemetry;
                return true;
            }
            catch (Exception ex)
            {
#if DEBUG
                ErrorHelpers.WriteLineMessage(
                    "WARNING:", "Failed to load telemetry config", ex.Message);
#endif
                return false;
            }
        }

        public readonly struct TelemetryConfig
        {
            public string Type { get; init; }

            public AriaTelemetryConfig Aria { get; init; }

            public readonly struct AriaTelemetryConfig
            {
                public string TenantId { get; init; }
            }
        }

        private readonly struct TelemetryConfigWrapper
        {
            public TelemetryConfig Telemetry { get; init; }
        }
    }
}

#if RELEASE
    #pragma warning restore CS0168 // Variable is declared but never used
#endif
