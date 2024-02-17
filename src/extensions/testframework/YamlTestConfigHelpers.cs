using System.IO;
using System.Linq;

namespace Azure.AI.Details.Common.CLI.TestFramework
{
    public class YamlTestConfigHelpers
    {
        public static FileInfo FindTestConfigFile(DirectoryInfo checkHereAndParents)
        {
            Logger.Log($"YamlTestConfigHelpers.GetTestConfigFile: Looking for test config file in {checkHereAndParents.FullName}");

            var configDirectory = checkHereAndParents.GetDirectories(YamlTestFramework.YamlTestsConfigDirectoryName).FirstOrDefault();
            var configFile = configDirectory?.GetFiles(YamlTestFramework.YamlTestsConfigFileName).FirstOrDefault();
            if (configFile?.Exists ?? false)
            {
                Logger.Log($"YamlTestConfigHelpers.GetTestConfigFile: Found test config file at {configFile.FullName}");
                return configFile;
            }

            return checkHereAndParents.Parent != null
                ? FindTestConfigFile(checkHereAndParents.Parent)
                : null;
        }

        public static DirectoryInfo GetTestDirectory(DirectoryInfo checkHereAndParents)
        {
            var file = FindTestConfigFile(checkHereAndParents);
            var tags = file != null ? YamlTagHelpers.GetTagsFromFile(file.FullName) : null;
            var testDirectory = tags?.ContainsKey("testDirectory") ?? false 
                ? tags["testDirectory"].FirstOrDefault()
                : null;

            if (testDirectory != null)
            {
                testDirectory = PathHelpers.Combine(file.Directory.FullName, testDirectory);
                Logger.Log($"YamlTestConfigHelpers.GetTestDirectory: Found test directory in config file at {testDirectory}");
                return new DirectoryInfo(testDirectory);
            }

            file = YamlTagHelpers.FindDefaultTagsFile(checkHereAndParents);
            tags = file != null ? YamlTagHelpers.GetTagsFromFile(file.FullName) : null;
            testDirectory = tags?.ContainsKey("testDirectory") ?? false
                ? tags["testDirectory"].FirstOrDefault()
                : null;

            if (testDirectory != null)
            {
                testDirectory = PathHelpers.Combine(file.Directory.FullName, testDirectory);
                Logger.Log($"YamlTestConfigHelpers.GetTestDirectory: Found test directory in default tags file at {testDirectory}");
                return new DirectoryInfo(testDirectory);
            }

            Logger.Log($"YamlTestConfigHelpers.GetTestDirectory: No test directory found; using {checkHereAndParents.FullName}");
            return checkHereAndParents;
        }
    }
}
