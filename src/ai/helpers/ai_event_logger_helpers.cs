//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
// using Azure.AI.Vision.Common.Diagnostics.Logging;

namespace Azure.AI.Details.Common.CLI
{
    public class AiEventLoggerHelpers : IEventLoggerHelpers
    {
        public void SetFilters(string autoExpectLogFilter)
        {
            // var filterLog = !string.IsNullOrEmpty(autoExpectLogFilter);
            // if (filterLog) EventLogger.SetFilters(autoExpectLogFilter.Split(';', StringSplitOptions.RemoveEmptyEntries));
        }

        public event EventHandler<string> OnMessage
        {
            add
            {
                // EventLogger.OnMessage += value;
            }
            remove
            {
                // EventLogger.OnMessage -= value;
            }
        }
    }
}
