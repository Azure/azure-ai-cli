#nullable enable

namespace Azure.AI.Details.Common.CLI.AzCli
{
    /// <summary>
    /// Information about a Cognitive Services AI resource (aka one with AI model deployments)
    /// </summary>
    /// <remarks>This is usually only used for AIServices, or OpenAI resource kind</remarks>
    public class CognitiveServicesAiResourceInfo : CognitiveServicesResourceInfo
    {
        /// <summary>
        /// The deployment information for the chat model
        /// </summary>
        public CognitiveServicesDeploymentInfo? ChatDeployment { get; set; }

        /// <summary>
        /// The deployment information for the embeddings model
        /// </summary>
        public CognitiveServicesDeploymentInfo? EmbeddingsDeployment { get; set; }

        /// <summary>
        /// The deployment information for the evaluation model
        /// </summary>
        public CognitiveServicesDeploymentInfo? EvaluationDeployment { get; set; }
    }
}
