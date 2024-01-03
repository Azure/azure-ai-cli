using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Azure.AI.OpenAI;
using DevLab.JmesPath.Utils;
using System.Linq;

namespace Azure.AI.Details.Common.CLI.Extensions.HelperFunctions
{
    public static class HelperFunctionFactoryExtensions
    {
        // extension method to ChatCompletionsOptions
        public static HelperFunctionCallContext AddFunctions(this ChatCompletionsOptions options, HelperFunctionFactory functionFactory)
        {
            foreach (var function in functionFactory.GetFunctionDefinitions())
            {
                // options.Tools.Add(new ChatCompletionsFunctionToolDefinition(function));
                options.Functions.Add(function);
            }

            return new HelperFunctionCallContext(functionFactory);
        }

        public static bool TryCallFunction(this ChatCompletionsOptions options, HelperFunctionCallContext context, out string? result)
        {
            return context.TryCallFunction(options, out result);
        }
    }
}
