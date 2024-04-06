//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System.Text;
using System.Text.Json;
using Azure.AI.Details.Common.CLI;
using Azure.AI.Details.Common.CLI.Telemetry;
using Microsoft.Applications.Events;

namespace Azure.AI.CLI.Telemetry.Aria
{
    /// <summary>
    /// An implementation that uses Aria for telemetry. See <see cref="https://www.aria.ms/help/FAQs/"/> for more information about Aria
    /// </summary>
    /// <remarks>This creates and maintains a JSON file in the .config user profile directory to store some information between
    /// runs for Aria. For example, a GUID is generated and used as the user ID for now the first time this code is run</remarks>
    public class AriaTelemetry : ITelemetry
    {
        private ILogger _logger;
        private AriaEventSerializer _serializer;
        private readonly AriaUserTelemetryConfig _userConfig;

        public AriaTelemetry(string tenantToken, string? userAgent)
        {
            if (string.IsNullOrWhiteSpace(tenantToken))
            {
                throw new ArgumentNullException(nameof(tenantToken));
            }

            EVTStatus status = LogManager.Start();
            ValidateStatus(status);

            _logger = LogManager.GetLogger(tenantToken, out status);
            ValidateStatus(status);

            _serializer = new AriaEventSerializer();
            _userConfig = GetOrCreateUserConfig();

            // set common event properties
            _logger.SetContext("UserId", _userConfig.UserId, Microsoft.Applications.Events.PiiKind.Identity);
            if (!string.IsNullOrWhiteSpace(userAgent))
            {
                _logger.SetContext("UserAgent", userAgent);
            }
        }

        public void LogEvent(ITelemetryEvent evt)
        {
            if (evt == null)
            {
                return;
            }

            EventProperties eventProps = _serializer.Serialize(evt);
            _logger.LogEvent(eventProps);
        }

        public async ValueTask DisposeAsync()
        {
            await LogManager.UploadNowAsync()
                .ConfigureAwait(false);

            LogManager.Teardown();
        }

        private static void ValidateStatus(EVTStatus status)
        {
            if (status == EVTStatus.OK || status == EVTStatus.AlreadyStarted)
            {
                // success
            }
            else
            {
                throw new ApplicationException("Failed to start Aria telemetry. ErrorCode: " + status);
            }
        }

        private static AriaUserTelemetryConfig GetOrCreateUserConfig()
        {
            var encoding = new UTF8Encoding(false, true);

            string ariaUserConfig = Path.Combine(
                FileHelpers.GetUserConfigDotDir(false),
                "config",
                "telemetry.user.aria.json");

            AriaUserTelemetryConfig? config = null;
            bool dirty = false;
            try
            {
                if (File.Exists(ariaUserConfig))
                {
                    config = JsonSerializer.Deserialize<AriaUserTelemetryConfig>(
                        File.ReadAllText(ariaUserConfig, encoding));
                }

                if (config == null)
                {
                    dirty = true;
                    config = new AriaUserTelemetryConfig()
                    {
                        UserId = string.Empty
                    };
                }

                if (!Guid.TryParse(config.UserId, out _))
                {
                    dirty = true;
                    config.UserId = Guid.NewGuid().ToString("D");
                }

                if (dirty)
                {
                    FileHelpers.WriteAllText(
                        ariaUserConfig,
                        JsonSerializer.Serialize(config),
                        encoding);
                }

                return config;
            }
            catch (Exception)
            {
                return config ?? new AriaUserTelemetryConfig()
                {
                    UserId = "00000000-0000-0000-0000-000000000000"
                };
            }
        }

        private class AriaUserTelemetryConfig
        {
            public required string UserId { get; set; }
            public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
        }
    }
}
