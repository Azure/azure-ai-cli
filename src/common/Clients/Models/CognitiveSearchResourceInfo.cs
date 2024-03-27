#nullable enable

namespace Azure.AI.Details.Common.CLI.AzCli
{
    /// <summary>
    /// Represents a Cognitive Search resource
    /// </summary>
    public class CognitiveSearchResourceInfo : ResourceInfoBase
    {
        /// <inheritdoc />
        public override ResourceKind Kind
        {
            get => ResourceKind.Search;
            init => _ = value; // do nothing
        }

        /// <summary>
        /// The endpoint for this search resource
        /// </summary>
        public string Endpoint =>
            // TODO: Need to find official way of getting this
            $"https://{Name}.search.windows.net";
    }
}
