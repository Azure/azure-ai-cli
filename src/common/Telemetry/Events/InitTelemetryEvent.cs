using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Azure.AI.Details.Common.CLI.Telemetry.Events
{
    public enum InitStage
    {
        Initial,
        Subscription,
        Resource,
        Project,
        Chat,
        Embedding,
        Evalution,
        Keys,
        Search
    }

    public readonly struct InitTelemetryEvent : ITelemetryEvent
    {
        private readonly string _name;

        public InitTelemetryEvent(InitStage stage)
        {
            _name = ToEventName(stage);
        }

        public string Name => _name;

        public string RunId { get; init; }

        public Outcome Outcome { get; init; }

        public string Selected { get; init; }

        public double DurationInMs { get; init; }

        public string Error { get; init; }

        private string ToEventName(InitStage stage)
        {
            var builder = new StringBuilder("init.stage.");
            bool first = true;

            foreach(var c in stage.ToString())
            {
                if (char.IsUpper(c))
                {
                    if (first) first = false;
                    else builder.Append("_");

                    builder.Append(char.ToLowerInvariant(c));
                }
                else if (c == '.')
                {
                    builder.Append("_");
                }
                else
                {
                    builder.Append(c);
                }
            }

            return builder.ToString();
        }
    }
}
