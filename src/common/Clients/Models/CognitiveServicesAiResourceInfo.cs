#nullable enable

namespace Azure.AI.Details.Common.CLI.AzCli
{
    /// <summary>
    /// Information about a Cognitive Services AI resource (aka one with AI model deployments)
    /// </summary>
    /// <remarks>This is usually only used for AIServices, or OpenAI resource kind</remarks>
    public class CognitiveServicesAiResourceInfo : CognitiveServicesResourceInfo
    {
        public CognitiveServicesDeploymentInfo? ChatDeployment { get; set; }
        public CognitiveServicesDeploymentInfo? EmbeddingsDeployment { get; set; }
        public CognitiveServicesDeploymentInfo? EvaluationDeployment { get; set; }
    }
}
