#nullable enable

using Azure.AI.CLI.Common.Clients.Models.Utils;
using Newtonsoft.Json;

namespace Azure.AI.Details.Common.CLI.AzCli
{
    public class CognitiveSearchResourceInfo : ResourceInfoBase
    {
        /// <inheritdoc />
        public override ResourceKind Kind
        {
            get => ResourceKind.Search;
            init => _ = value; // do nothing
        }

        public string Endpoint =>
            // TODO: Need to find official way of getting this
            $"https://{Name}.search.windows.net";
    }
}
