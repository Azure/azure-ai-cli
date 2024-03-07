//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;

namespace Azure.AI.Details.Common.CLI.TestFramework
{
    public class TimeSpanFormatter
    {
        public static string FormatMsOrSeconds(TimeSpan timeSpan)
        {
            var ms = timeSpan.TotalMilliseconds;
            var secs = ms / 1000;
            var duration = ms >= 1000
                ? secs.ToString("0.00") + " seconds"
                : ms.ToString("0") + " ms";
            return duration;
        }
    }
}

