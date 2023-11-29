using Azure.AI.Details.Common.CLI.Extensions.HelperFunctions;

public static class HelperFunctionClass1
{
    [HelperFunctionDescription("Gets the user's name")]
    public static string Add()
    {
        return Environment.UserName;
    }
}