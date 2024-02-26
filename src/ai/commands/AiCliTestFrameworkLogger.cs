//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using Azure.AI.Details.Common.CLI.TestFramework;

namespace Azure.AI.Details.Common.CLI
{
    public class AiCliTestFrameworkLogger : IYamlTestFrameworkLogger
    {
        public void LogVerbose(string text)
        {
            AI.TRACE_VERBOSE(text);
        }

        public void LogInfo(string text)
        {
            AI.TRACE_INFO(text);
        }

        public void LogWarning(string text)
        {
            AI.TRACE_WARNING(text);
        }

        public void LogError(string text)
        {
            AI.TRACE_ERROR(text);
        }
    }
}
