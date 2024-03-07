//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace Azure.AI.Details.Common.CLI.TestFramework
{
    public class YamlTestFrameworkHandleHost : IYamlTestFrameworkHost
    {
        private readonly IFrameworkHandle _frameworkHandle;

        public YamlTestFrameworkHandleHost(IFrameworkHandle frameworkHandle)
        {
            _frameworkHandle = frameworkHandle;
        }

        public void RecordStart(TestCase testCase)
        {
            _frameworkHandle.RecordStart(testCase);
        }

        public void RecordResult(TestResult testResult)
        {
            _frameworkHandle.RecordResult(testResult);
        }

        public void RecordEnd(TestCase testCase, TestOutcome outcome)
        {
            _frameworkHandle.RecordEnd(testCase, outcome);
        }
    }
}
