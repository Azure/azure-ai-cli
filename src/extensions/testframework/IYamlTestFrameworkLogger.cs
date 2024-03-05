//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

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
