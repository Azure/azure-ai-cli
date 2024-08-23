//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Azure.AI.Details.Common.CLI.TestFramework
{
    public class RunnableTestCaseItem
    {
        public RunnableTestCaseItem(RunnableTestCase runnableTest, Dictionary<string, string>? properties = null)
        {
            var matrixId = properties != null ? YamlTestCaseMatrixHelpers.GetMatrixId(properties) : null;
            _matrixId = matrixId ?? Guid.NewGuid().ToString();

            var testId = runnableTest.Test.Id.ToString();
            _id = ItemIdFromIds(testId, _matrixId);

            _runnableTest = runnableTest;
            _properties = properties;

            _cli = GetInterpolatedProperty("cli") ?? "";
            _command = GetInterpolatedProperty("command");
            _script = GetInterpolatedProperty("script");

            var bash = GetInterpolatedProperty("bash");
            _scriptIsBash = !string.IsNullOrEmpty(bash);
            if (_scriptIsBash) _script = bash;

            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            if (!isWindows) _scriptIsBash = true;

            _arguments = GetInterpolatedProperty("arguments", escapeJson: true);
            _input = GetInterpolatedProperty("input");
            if (_input != null && _input.StartsWith('"') && _input.EndsWith('"')) _input = _input[1..^1];

            _expectGpt = GetInterpolatedProperty("expect");
            _expectRegex = GetInterpolatedProperty("expect-regex");
            _notExpectRegex = GetInterpolatedProperty("not-expect-regex");

            _env = GetInterpolatedProperty("env");
            _workingDirectory = GetInterpolatedProperty("working-directory");

            _timeout = int.Parse(GetInterpolatedProperty("timeout", YamlTestFramework.DefaultTimeout)!);
            _skipOnFailure = GetInterpolatedProperty("skipOnFailure") switch { "true" => true, _ => false };

            var basePath = new FileInfo(_runnableTest.Test.CodeFilePath).DirectoryName!;
            _workingDirectory = Path.Combine(basePath, _workingDirectory ?? "");
            var tryCreateWorkingDirectory = !string.IsNullOrEmpty(_workingDirectory) && !Directory.Exists(_workingDirectory);
            if (tryCreateWorkingDirectory) Directory.CreateDirectory(_workingDirectory!);

            _foreach = GetInterpolatedProperty("foreach", escapeJson: true);
        }

        public string Id { get { return _id; } }
        public string MatrixId { get { return _matrixId; } }

        public static string ItemIdFromIds(string testId, string matrixId)
        {
            return $"{testId}.{matrixId}";
        }

        public RunnableTestCase RunnableTest { get { return _runnableTest; } }

        public IList<TestResult> RunAndRecord(IYamlTestFrameworkHost host)
        {
            _runnableTest.RecordStart(host, this);
    
            // run the test case, getting all the results, prior to recording any of those results
            // (not doing this in this order seems to, for some reason, cause "foreach" test cases to run 5 times!?)
            var results = YamlTestCaseRunner.TestCaseGetResults(_runnableTest.Test, _cli, _command, _script, _scriptIsBash, _arguments, _input, _expectGpt, _expectRegex, _notExpectRegex, _env, _workingDirectory, _timeout, _skipOnFailure, _foreach);
            _results = results.ToList();

            _runnableTest.RecordResults(host, this, _results);
            _runnableTest.RecordStop(host, this);

            return _results;
        }

        private string? GetInterpolatedProperty(string key, string? defaultValue = null, bool escapeJson = false)
        {
            var value = YamlTestProperties.Get(_runnableTest.Test, key, defaultValue);
            return Interpolate(value, escapeJson);
        }

        public string? Interpolate(string? text, bool escapeJson = false)
        {
            return _properties != null
                ? PropertyInterpolationHelpers.Interpolate(text!, _properties, escapeJson)
                : text;
        }

        private string _id;
        private string _matrixId;
        private RunnableTestCase _runnableTest;
        private Dictionary<string, string>? _properties;

        private string _cli;
        private string? _command;
        private string? _script;
        private bool _scriptIsBash;
        private string? _arguments;
        private string? _input;
        private string? _expectGpt;
        private string? _expectRegex;
        private string? _notExpectRegex;
        private string? _env;
        private string? _workingDirectory;
        private int _timeout;
        private bool _skipOnFailure;
        private string? _foreach;

        private List<TestResult>? _results;
    }
}
