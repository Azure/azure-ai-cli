﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Azure.AI.Details.Common.CLI.Telemetry.Events
{
    [DebuggerDisplay("{Name}: {ExitCode}")]
    public readonly struct ExitedTelemetryEvent : ITelemetryEvent
    {
        public string Name => "exited";

        public int ExitCode { get; init; }

        public TimeSpan? Elapsed { get; init; }
    }
}
