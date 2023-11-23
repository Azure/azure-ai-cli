<#@ template hostspecific="true" #>
<#@ output extension=".cs" encoding="utf-8" #>
<#@ parameter type="System.String" name="Name" #>
using Azure.AI.Details.Common.CLI.Extensions.HelperFunctions;

public static class <#= Name #>
{
    [HelperFunctionDescription("Gets the user's name")]
    public static string Add()
    {
        return Environment.UserName;
    }
}
