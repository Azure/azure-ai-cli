using System.Diagnostics;
using System.Text;

namespace Azure.AI.Details.Common.CLI.Telemetry.Events
{
    /// <summary>
    /// What step of the init command we are in
    /// </summary>
    public enum InitStage
    {
        /// <summary>
        /// The user has made a selection
        /// </summary>
        Choice,

        /// <summary>
        /// The user has chosen a subscription
        /// </summary>
        Subscription,

        /// <summary>
        /// The user has chosen a resource. Refer to the <see cref="InitTelemetryEvent.Selected"> property to see
        /// which kind of resource was selected
        /// </summary>
        Resource,

        /// <summary>
        /// (Optional) The user has chosen an AI project
        /// </summary>
        Project,

        /// <summary>
        /// (Optional) The user has made a choice regarding a chat deployment. <see cref="InitTelemetryEvent.Selected">
        /// property to see what was chosen (e.g. new, existing, skip)
        /// </summary>
        Chat,

        /// <summary>
        /// (Optional) The user has made a choice regarding an embedding deployment. <see cref="InitTelemetryEvent.Selected">
        /// property to see what was chosen (e.g. new, existing, skip)
        /// </summary>
        Embeddings,

        /// <summary>
        /// (Optional) The user has made a choice regarding an evaluation deployment. <see cref="InitTelemetryEvent.Selected">
        /// property to see what was chosen (e.g. new, existing, skip)
        /// </summary>
        Evaluation,

        /// <summary>
        /// (Optional) Connections have been verified or new ones have been created
        /// </summary>
        Connections,

        /// <summary>
        /// (Optional) We have successfully saved the configuration
        /// </summary>
        Save
    }

    /// <summary>
    /// Represents details about the init command. This is used to generate (pseudo-)funnels in the dashboards to get an
    /// idea of where end users are encountering issues or giving up
    /// </summary>
    [DebuggerDisplay("{Name}: {Stage} {Outcome} {Selected}")]
    public readonly struct InitTelemetryEvent : ITelemetryEvent
    {
        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="stage"></param>
        public InitTelemetryEvent(InitStage stage)
        {
            Stage = stage;
        }

        /// <summary>
        /// The name of this event
        /// </summary>
        public string Name => "init.stage";

        /// <summary>
        /// The stage in the init process
        /// </summary>
        public InitStage Stage { get; }

        /// <summary>
        /// A unique identifier for this particular run of the init command. This can be used to uniquely identify
        /// a single sequence of events
        /// </summary>
        public string? RunId { get; init; }

        /// <summary>
        /// The type of init run we are doing. Due to subtle differences in the various options, there are currently
        /// 3 kinds: new, existing, standalone_{resType}
        /// </summary>
        public string? RunType { get; init; }

        /// <summary>
        /// The outcome of this step
        /// </summary>
        public Outcome Outcome { get; init; }

        /// <summary>
        /// (Optional) Some additional information about what was selected (e.g. new, existing, skipped)
        /// </summary>
        public string? Selected { get; init; }

        /// <summary>
        /// How long the user spent in that particular stage
        /// </summary>
        public double DurationInMs { get; init; }

        /// <summary>
        /// (Optional) Any error information encountered that lead to a failed outcome
        /// </summary>
        public string? Error { get; init; }
    }
}
