//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Azure.AI.Details.Common.CLI.TestFramework
{
    public class TestRun
    {
        public void StartTest(TestCase testCase, Guid? guid = null)
        {
            _startTime ??= DateTime.Now;
            _testCases.Add(testCase);
            SetExecutionId(testCase, guid ?? Guid.NewGuid());
        }

        public void RecordTest(TestResult testResult)
        {
            _testResults.Add(testResult);
        }

        public void EndTest(TestCase testCase, TestOutcome outcome)
        {
            _endTime = DateTime.Now;
        }

        public void EndRun()
        {
            var now = DateTime.Now;
            _startTime ??= now;
            _endTime = now;
        }

        public IList<TestCase> TestCases => _testCases.ToList();

        public IList<TestResult> TestResults => _testResults.ToList();

        public TimeSpan Duration
        {
            get 
            {
                return _startTime != null && _endTime != null
                    ? (_endTime.Value - _startTime.Value)
                    : TimeSpan.Zero;
            }
        }

        public DateTime StartTime => _startTime ?? throw new InvalidOperationException("RunStartTime is not set");

        public DateTime EndTime => _endTime ?? throw new InvalidOperationException("RunEndTime is not set");

        public Guid GetExecutionId(TestCase testCase)
        {
            lock (_testToExecutionMap)
            {
                return _testToExecutionMap[testCase.Id];
            }
        }

        private void SetExecutionId(TestCase testCase, Guid guid)
        {
            lock (_testToExecutionMap)
            {
                _testToExecutionMap[testCase.Id] = guid;
            }
        }

        private DateTime? _startTime;
        private DateTime? _endTime;

        private List<TestCase> _testCases = new List<TestCase>();
        private Dictionary<Guid, Guid> _testToExecutionMap = new Dictionary<Guid, Guid>();
        private List<TestResult> _testResults = new List<TestResult>();
    }
}

