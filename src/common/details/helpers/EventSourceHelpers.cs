//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System.Diagnostics.Tracing;

namespace Azure.AI.Details.Common.CLI
{
    public class EventSourceHelpers
    {
        public static void EventSourceAiLoggerLog(EventWrittenEventArgs e, string message)
        {
            message = message.Replace("\r", "\\r").Replace("\n", "\\n");
            switch (e.Level)
            {
                case EventLevel.Error:
                    AI.DBG_TRACE_ERROR(message, 0, e.EventSource.Name, e.EventName);
                    break;

                case EventLevel.Warning:
                    AI.DBG_TRACE_WARNING(message, 0, e.EventSource.Name, e.EventName);
                    break;

                case EventLevel.Informational:
                    AI.DBG_TRACE_INFO(message, 0, e.EventSource.Name, e.EventName);
                    break;

                default:
                case EventLevel.Verbose:
                    AI.DBG_TRACE_VERBOSE(message, 0, e.EventSource.Name, e.EventName); break;
            }
        }
    }
}
