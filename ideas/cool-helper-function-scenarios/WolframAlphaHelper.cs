//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using Azure.AI.Details.Common.CLI.Extensions.HelperFunctions;
using System;
using System.Net.Http;
using System.Threading.Tasks;

public static class WolframAlphaHelper
{
    private static readonly HttpClient client = new HttpClient();

    [HelperFunctionDescription("Calls Wolfram Alpha to answer a calculus question")]
    public static async Task<string> AnswerCalculusQuestion(string question)
    {
        string wolframAlphaApiKey = "Place-Holder";
        string requestUri = $"http://api.wolframalpha.com/v2/query?input={Uri.EscapeDataString(question)}&appid={wolframAlphaApiKey}";

        HttpResponseMessage response = await client.GetAsync(requestUri);
        response.EnsureSuccessStatusCode();

        string responseBody = await response.Content.ReadAsStringAsync();
        return responseBody;
    }
}