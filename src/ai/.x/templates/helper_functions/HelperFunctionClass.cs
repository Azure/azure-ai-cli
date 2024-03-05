//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

<#@ template hostspecific="true" #>
<#@ output extension=".cs" encoding="utf-8" #>
<#@ parameter type="System.String" name="ClassName" #>
using Azure.AI.Details.Common.CLI.Extensions.HelperFunctions;

public static class <#= ClassName #>
{
    [HelperFunctionDescription("Gets the user's name")]
    public static string GetUsersName()
    {
        return Environment.UserName;
    }
}
