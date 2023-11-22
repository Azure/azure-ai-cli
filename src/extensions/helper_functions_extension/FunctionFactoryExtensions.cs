using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Azure.AI.OpenAI;
using DevLab.JmesPath.Utils;
using System.Linq;

namespace Azure.AI.Details.Common.CLI.Extensions.HelperFunctions
{
    public static class FunctionFactoryExtensions
    {
        // extension method to ChatCompletionsOptions
        public static FunctionCallContext AddFunctions(this ChatCompletionsOptions options, FunctionFactory functionFactory)
        {
            foreach (var function in functionFactory.GetFunctionDefinitions())
            {
                options.Functions.Add(function);
            }

            return new FunctionCallContext(functionFactory);
        }

        public static bool TryCallFunction(this ChatCompletionsOptions options, FunctionCallContext context, out string? result)
        {
            return context.TryCallFunction(options, out result);
        }
    }
}
