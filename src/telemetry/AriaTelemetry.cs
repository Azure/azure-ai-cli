//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using Microsoft.Applications.Events;

namespace Azure.AI.Details.Common.CLI.Telemetry
{
    public class AriaTelemetry : ITelemetry
    {
        private static int _logManagerStarted = 0;
        private ILogger _logger;
        private AriaEventSerializer _serializer;
        private readonly string _userAgent;

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
        }

        public ValueTask LogEventAsync(ITelemetryEvent evt, CancellationToken token)
        {
            if (evt == null || token.IsCancellationRequested)
            {
                return ValueTask.CompletedTask;
            }

            EventProperties eventProps = _serializer.Serialize(evt);
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
    }
}
