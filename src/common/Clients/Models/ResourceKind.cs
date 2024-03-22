#nullable enable

namespace Azure.AI.Details.Common.CLI.AzCli
{
    /// <summary>
    /// Possible supported kinds of Azure resources
    /// </summary>
    public enum ResourceKind
    {
        /// <summary>
        /// Unknown kind
        /// </summary>
        Unknown,

        /// <summary>
        /// An AI hub resource (this is basically way to collect an AI resource of some kind [e.g. AIServices], with insights, key vault, and other related
        /// resources)
        /// </summary>
        Hub,

        /// <summary>
        /// An AI project. An AI hub resource can have one or more AI projects associated with it
        /// </summary>
        Project,

        /// <summary>
        /// An AI services resource (OpenAI, speech, vision, etc...)
        /// </summary>
        AIServices,

        /// <summary>
        /// A "legacy" ai resource (Speech, vision, etc...). Note this has no OpenAI support
        /// </summary>
        CognitiveServices,

        /// <summary>
        /// A "legacy" Azure OpenAI resource. This does only OpenAI
        /// </summary>
        OpenAI,

        /// <summary>
        /// A "legacy" speech resource. This does only speech
        /// </summary>
        Speech,

        /// <summary>
        /// A "legacy" vision resource. This does only vision
        /// </summary>
        Vision,

        /// <summary>
        /// A search resource
        /// </summary>
        Search,

        /// <summary>
        /// A "legacy" face subscription
        /// </summary>
        Face,
    }
}
