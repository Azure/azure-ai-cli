//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System.Text;
using System.Text.Json;
using Microsoft.Applications.Events;

namespace Azure.AI.Details.Common.CLI.Telemetry
{
    public class AriaTelemetry : ITelemetry
    {
        private ILogger _logger;
        private AriaEventSerializer _serializer;
        private readonly string _userAgent;
        private readonly AriaUserTelemetryConfig _userConfig;

        public AriaTelemetry(string tenantToken, IProgramData programData)
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
            _userAgent = programData?.TelemetryUserAgent ?? string.Empty;
            _userConfig = GetOrCreateUserConfig();
        }

        public ValueTask LogEventAsync(ITelemetryEvent evt, CancellationToken token)
        {
            if (evt == null || token.IsCancellationRequested)
            {
                return ValueTask.CompletedTask;
            }

            EventProperties eventProps = _serializer.Serialize(evt);
            eventProps.SetProperty("UserId", _userConfig.UserId, Microsoft.Applications.Events.PiiKind.Identity);
            if (_userAgent != null)
            {
                eventProps.SetProperty("UserAgent", _userAgent);
            }

            _logger.LogEvent(eventProps);

            return ValueTask.CompletedTask;
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
