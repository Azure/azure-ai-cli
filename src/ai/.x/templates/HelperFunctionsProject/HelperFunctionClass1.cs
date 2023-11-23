<#@ template hostspecific="true" #>
<#@ parameter type="System.String" name="Fred" #>

using Azure.AI.Details.Common.CLI.Extensions.HelperFunctions;

public static class HelperFunctionClass1
{
    [HelperFunctionDescription("Gets the user's name")]
    public static string Add()
    {
<# if (Fred == null) { #>
        return Environment.UserName;
<# } else { #>
        return Environment.UserName + " <#= Fred #>";
<# } #>
    }
}
