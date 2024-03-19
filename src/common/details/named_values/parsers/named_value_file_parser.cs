//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System.IO;
using System.Collections.Generic;

namespace Azure.AI.Details.Common.CLI
{
    public class PromptyFrontMatter
    {
        public string Schema { get; set; }
        public string Api { get; set; } = "chat";
        public string Name { get; set; }
        public string Description { get; set; }
        public string Version { get; set; }
        public List<string> Authors { get; set; }
        public List<string> Tags { get; set; }
        public string Base { get; set; }
        public IModel Model { get; set; }
        public Dictionary<string, string> Inputs { get; set; }
        //public string Template { get; set; } = "f-string";
        public ModelParameters Parameters { get; set; }
    }

    public interface IModel { }

    public class OpenAIModel : IModel
    {
        public string Type { get; set; } = "openai";
        public string Name { get; set; }
        public string Organization { get; set; }
    }

    public class AzureOpenAIModel : IModel
    {
        public string Type { get; set; } = "azure_openai";
        public string ApiVersion { get; set; }
        public string AzureDeployment { get; set; }
        public string AzureEndpoint { get; set; }
    }

    public class MaasModel : IModel
    {
        public string Type { get; set; } = "model_as_a_service";
        public string AzureEndpoint { get; set; }
    }

    public class ModelParameters
    {
        public object ResponseFormat { get; set; }
        public int Seed { get; set; }
        public int MaxTokens { get; set; }
        public double Temperature { get; set; }

        public List<object> Tools { get; set; }
        public double FrequencyPenalty { get; set; }
        public double PresencePenalty { get; set; }
        public List<string> Stop { get; set; }
        public double TopP { get; set; }
    }

    public abstract class INamedValueFileParser
    {
        public abstract bool Parse(string path, INamedValues values);
    }

    public class NamedValuePromptyFileParser : INamedValueFileParser
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public NamedValuePromptyFileParser()
        {
        }

        public override bool Parse(string path, INamedValues values)
        {
            bool parsed = false;
            return parsed;
        }
    }
}
