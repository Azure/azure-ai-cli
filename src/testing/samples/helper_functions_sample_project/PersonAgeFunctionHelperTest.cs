//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using Azure.AI.OpenAI;
using Azure.AI.Details.Common.CLI.Extensions.HelperFunctions;

namespace Azure.AI.Details.Common.CLI.Extensions.HelperFunctions.Test
{
    public static class PersonAgeFunctionHelperTest
    {
        [HelperFunctionDescription("Returns the age of a person")]
        public static int GetPersonAge([HelperFunctionParameterDescription("The name of the person whose age to return; Names must be capitalized correctly")] string personName)
        {
            return personName switch
            {
                "Beckett" => 17,
                "Jac" => 19,
                "Kris" => 53,
                "Rob" => 54,
                _ => 0,
            };
        }
    }
}
