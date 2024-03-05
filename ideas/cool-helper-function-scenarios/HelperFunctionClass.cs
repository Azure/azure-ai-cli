//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using Azure.AI.Details.Common.CLI.Extensions.HelperFunctions;

public static class HelperFunctionClass
{
    [HelperFunctionDescription("Gets the user's name")]
    public static string GetUsersName()
    {
        return Environment.UserName;
    }
}