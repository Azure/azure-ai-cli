//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System.Text;
using Azure.AI.OpenAI;

namespace Azure.AI.Details.Common.CLI.Extensions.HelperFunctions
{
    public static class DateTimeHelpers
    {
        [HelperFunctionDescription("Gets the current date and time")]
        public static string GetCurrentDateTime()
        {
            return DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        }
    }
}
