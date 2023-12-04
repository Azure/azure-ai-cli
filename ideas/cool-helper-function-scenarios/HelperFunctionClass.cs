using Azure.AI.Details.Common.CLI.Extensions.HelperFunctions;

public static class HelperFunctionClass
{
    [HelperFunctionDescription("Gets the user's name")]
    public static string GetUsersName()
    {
        return Environment.UserName;
    }
}