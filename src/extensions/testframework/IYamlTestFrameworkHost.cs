//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Azure.AI.Details.Common.CLI.TestFramework
{
    public interface IYamlTestFrameworkHost
    {
        void RecordStart(TestCase testCase);
        void RecordResult(TestResult testResult);
        void RecordEnd(TestCase testCase, TestOutcome outcome);
    }
}
