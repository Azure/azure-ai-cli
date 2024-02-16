using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Azure.AI.Details.Common.CLI.TestFramework
{
    public class TestResultHelpers
    {
        public static TestOutcome TestOutcomeFromResults(IEnumerable<TestResult> results)
        {
            var failed = results.Count(x => x.Outcome == TestOutcome.Failed) > 0;
            var skipped = results.Count(x => x.Outcome == TestOutcome.Skipped) > 0;
            var notFound = results.Count(x => x.Outcome == TestOutcome.NotFound) > 0 || results.Count() == 0;

            return failed ? TestOutcome.Failed
                : skipped ? TestOutcome.Skipped
                : notFound ? TestOutcome.NotFound
                : TestOutcome.Passed;
        }
    }
}
