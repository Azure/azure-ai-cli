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

