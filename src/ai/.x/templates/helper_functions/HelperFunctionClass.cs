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
