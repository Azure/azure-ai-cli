namespace Azure.AI.Details.Common.CLI.TestFramework
{
    public interface IYamlTestFrameworkLogger
    {
        void LogVerbose(string text);
        void LogInfo(string text);
        void LogWarning(string text);
        void LogError(string text);
    }
}
