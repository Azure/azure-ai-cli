//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO.Compression;
using System.Runtime.InteropServices;
using DevLab.JmesPath;

namespace Azure.AI.Details.Common.CLI
{
    public class CommandRunDispatcher
    {
        public static bool DispatchRunCommand(ICommandValues values)
        {
            var saveAsFile = values["x.command.save.as.file"];
            if (!string.IsNullOrEmpty(saveAsFile))
            {
                values.Reset("x.command.save.as.file");
                return SaveCommand(values, saveAsFile);
            }

            var zipAsFile = values["x.command.zip.as.file"];
            if (!string.IsNullOrEmpty(zipAsFile))
            {
                values.Reset("x.command.zip.as.file");
                return ZipCommand(values, zipAsFile);
            }

            var time = values.Get("x.run.time");
            if (string.IsNullOrEmpty(time)) values.Add("x.run.time", DateTime.Now.ToFileTime().ToString());

            var queue = new Queue<ICommandValues>();
            queue.Enqueue(values);

            return DispatchRunCommand(values, queue, true, true);
        }

        public static bool DispatchRunCommand(ICommandValues values, Queue<ICommandValues>? queue, bool expandOk, bool parallelOk)
        {
            var command = values.GetCommand();

            if (command == "config")
            {
                return ConfigCommand.RunCommand(values);
            }
            else if (expandOk || parallelOk || queue != null)
            {
                return Command.RunCommand(values, queue, expandOk, parallelOk);
            }

            return Program.DispatchRunCommand(values);
        }

        public static bool SaveCommand(ICommandValues values, string saveAsFile)
        {
            var fileNames = values.SaveAs(saveAsFile).Split(';');
            for (int i = 0; i < fileNames.Length; i++)
            {
                Console.WriteLine(i == 0 ? $"Saved: {fileNames[i]}" : $"  and: {fileNames[i]}");
            }
            return true;
        }

        public static bool ZipCommand(ICommandValues values, string zipAsFile)
        {
            var verbose = values.GetOrDefault("x.verbose", true);
            if (verbose) Console.WriteLine($"Zipping into {zipAsFile} ...\n");

            if (!zipAsFile.EndsWith(".zip")) zipAsFile = zipAsFile + ".zip";

            var tempDirectory = PathHelpers.Combine(Path.GetTempPath(), zipAsFile + DateTime.Now.ToFileTime().ToString() + ".temp")!;
            Directory.CreateDirectory(tempDirectory);

            ZipCommand(values, zipAsFile, tempDirectory, verbose);

            if (verbose) Console.WriteLine("  Completed!\n");
            if (verbose) Console.WriteLine("  NOTE: Remember to manually include input files (audio, models, ...)\n");

            return true;
        }

        private static void ZipCommand(ICommandValues values, string zipAsFile, string tempDirectory, bool verbose)
        {
            if (File.Exists(zipAsFile)) ZipFile.ExtractToDirectory(zipAsFile, tempDirectory);

            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            var zipTarget = values.GetOrEmpty("x.command.zip.target");
            var targetWebJob = zipTarget.ToLower() == "webjob";

            // run.cmd or run.sh
            var runScript = isWindows ? "run.cmd" : "run.sh";
            if (verbose) Console.WriteLine($"  Saving {runScript}... ");

            var command = values.GetCommand();
            var commandJob = "./" + command + ".job";

            var runScriptPath = PathHelpers.Combine(tempDirectory, runScript)!;

            if (targetWebJob)
            {
                var prefix = $"{Program.Name.ToUpper()}_X";
                var setOutputPath = isWindows
                    ? $"set {prefix}_OUTPUT_PATH=\nif not '%WEBJOBS_DATA_PATH%'=='' set {prefix}_OUTPUT_PATH=%WEBJOBS_DATA_PATH%\\%WEBJOBS_RUN_ID%"
                    : $"{prefix}_OUTPUT_PATH=; if [ ! -z \"$WEBJOBS_DATA_PATH\" ]; then {prefix}_OUTPUT_PATH=$(WEBJOBS_DATA_PATH)/$(WEBJOBS_RUN_ID); fi;";
                FileHelpers.WriteAllText(runScriptPath, setOutputPath + Environment.NewLine, Encoding.Default);
            }

            var app = isWindows ? Program.Name : $"./{Program.Name}";
            FileHelpers.AppendAllText(runScriptPath, $"{app} {command} --nodefaults @{commandJob}" + Environment.NewLine, Encoding.Default);

            // settings.job
            if (targetWebJob)
            {
                var settingsPath = "settings.job";
                if (verbose) Console.WriteLine($"  Saving {settingsPath}... ");

                var settingsJson = "{ \"is_in_place\": true }";
                settingsPath = PathHelpers.Combine(tempDirectory, "settings.job")!;
                FileHelpers.WriteAllText(settingsPath, settingsJson, Encoding.Default);
            }

            // ./{command}.job and related files
            var files = values.SaveAs(commandJob).Split(';');
            foreach (var file in files)
            {
                FileHelpers.TryCopyFile(".", file, tempDirectory, null, verbose);
                File.Delete(file);
            }

            // binaries
            var sourcePath = AppContext.BaseDirectory;

            var programExe = OperatingSystem.IsWindows() ? Program.Exe : Program.Exe.Replace(".exe", "");
            FileHelpers.TryCopyFile(sourcePath, programExe, tempDirectory, null, verbose);

            #if NETCOREAPP
            FileHelpers.TryCopyFile(sourcePath, Program.Name, tempDirectory, null, verbose);
            FileHelpers.TryCopyFile(sourcePath, Program.Dll, tempDirectory, null, verbose);
            FileHelpers.TryCopyFile(sourcePath, "runtimeconfig.json", tempDirectory, null, verbose);
            FileHelpers.TryCopyFile(sourcePath, $"{Program.Name}.runtimeconfig.json", tempDirectory, null, verbose);
            #endif

            var bindingAssembly = FileHelpers.GetAssemblyFileInfo(Program.BindingAssemblySdkType);
            sourcePath = bindingAssembly.DirectoryName!;
            FileHelpers.TryCopyFile(sourcePath, bindingAssembly.Name, tempDirectory, null, verbose);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string[] locations = { "", "runtimes/win-x64/native/", "../runtimes/win-x64/native/" };
                FileHelpers.TryCopyFiles(PathHelpers.Combine(sourcePath, locations), Program.ZipIncludes, tempDirectory, null, null, verbose);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                string[] locations = { "", "runtimes/linux-x64/native/", "../../runtimes/linux-x64/native/" };
                FileHelpers.TryCopyFiles(PathHelpers.Combine(sourcePath, locations), Program.ZipIncludes, tempDirectory, "lib", ".so", verbose);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                string[] locations = { "", "runtimes/osx-x64/native/", "../../runtimes/osx-x64/native/" };
                FileHelpers.TryCopyFiles(PathHelpers.Combine(sourcePath, locations), Program.ZipIncludes, tempDirectory, "lib", ".dylib", verbose);
            }

            // Dependent type assemblies (Linq Async, System.Interactive.Async and JMESPath)
            // These assemblies are embedded in spx.exe when PublishAsSingleFile is set (Dependency package)
            Type[] types = { typeof(System.Linq.AsyncEnumerable), typeof(System.Linq.AsyncEnumerableEx), typeof(DevLab.JmesPath.Parser), typeof(DevLab.JmesPath.JmesPath)};
            foreach (var t in types)
            {
                var fi = FileHelpers.GetAssemblyFileInfo(t);
                FileHelpers.TryCopyFile(fi.DirectoryName!, fi.Name, tempDirectory, null, verbose);
            }

            File.Delete(zipAsFile);
            ZipFile.CreateFromDirectory(tempDirectory, zipAsFile);

            Directory.Delete(tempDirectory, true);
        }
    }

    public class Command
    {
        public Command(ICommandValues values)
        {
            _values = values;
            _values.ReplaceValues();
        }

        public static bool RunCommand(ICommandValues values, Queue<ICommandValues>? queue, bool expandOk = true, bool parallelOk = true)
        {
            if (expandOk) return CheckExpandRunCommand(values, queue, expandOk, parallelOk);
            if (parallelOk) return CheckParallelRunCommand(values, queue, expandOk, ref parallelOk);

            return queue != null && CheckExpectedRunInProcAsync(values, queue).Result;
        }

        protected static bool CheckExpandRunCommand(ICommandValues values, Queue<ICommandValues>? queue, bool expandOk, bool parallelOk)
        {
            bool passed = false;

            var fileValueName = values.GetOrEmpty("x.command.expand.file.name");
            var files = values.GetOrEmpty($"{fileValueName}s");

            var forEachCount = values["foreach.count"];
            var repeat = values["x.command.repeat"];

            if (!string.IsNullOrEmpty(files))
            {
                BlockValues(values, queue!, $"{fileValueName}s");
                passed = CommandRunDispatcher.DispatchRunCommand(values, ExpandQueueForEachFile(queue!, fileValueName, files), expandOk, parallelOk);
            }
            else if (!string.IsNullOrEmpty(forEachCount))
            {
                BlockValues(values, queue!, "foreach.count");
                passed = CommandRunDispatcher.DispatchRunCommand(values, ExpandQueueForEachTsv(values, queue!, forEachCount), expandOk, parallelOk);
            }
            else if (!string.IsNullOrEmpty(repeat))
            {
                BlockValues(values, queue!, "x.command.repeat");
                passed = CommandRunDispatcher.DispatchRunCommand(values, ExpandQueueFromRepeatCount(queue!, repeat), expandOk, parallelOk);
            }
            else
            {
                var max = values.GetOrDefault("x.command.max", int.MaxValue);
                if (queue != null && queue.Count > max) queue = new Queue<ICommandValues>(queue.Take(max));

                expandOk = false;
                passed = CommandRunDispatcher.DispatchRunCommand(values, queue, expandOk, parallelOk);
            }

            return passed;
        }

        protected static void BlockValues(INamedValues values, string name)
        {
            values.Reset(name);
            values.Add(name, null);
        }

        protected static void BlockValues(INamedValues values, Queue<ICommandValues> queue, string name)
        {
            BlockValues(values, name);
            foreach (var item in queue)
            {
                BlockValues(item, name);
            }
        }

        protected static Queue<ICommandValues> ExpandQueueFromRepeatCount(Queue<ICommandValues> queue, string repeat)
        {
            Queue<ICommandValues> newQueue = new Queue<ICommandValues>();

            var count = Int32.Parse(repeat);
            for (int i = 0; i < count; i++)
            {
                foreach (var item in queue)
                {
                    var values = new CommandValues(item);
                    newQueue.Enqueue(values);
                }
            }

            return newQueue;
        }

        protected static Queue<ICommandValues> ExpandQueueForEachFile(Queue<ICommandValues> queue, string fileValueName, string files)
        {
            Queue<ICommandValues> newQueue = new Queue<ICommandValues>();

            foreach (var item in queue)
            {
                var list = FileHelpers.FindFiles(files, item, httpLinksOk: true);
                foreach (var fileValue in list)
                {
                    var values = CombineValuesWithNameValue(item, fileValueName, fileValue);
                    newQueue.Enqueue(values);
                }

                if (newQueue.Count == 0 && Program.Debug)
                {
                    Console.WriteLine($"No files found for {fileValueName} in {files}");
                }
            }

            return newQueue;
        }

        protected static Queue<ICommandValues> ExpandQueueForEachTsv(INamedValues tsvOptions, Queue<ICommandValues> queue, string forEachCount)
        {
            var count = int.Parse(forEachCount);
            for (int i = 0; i < count; i++)
            {
                var forEachTsvFile = $"foreach.{i}.tsv.file";

                var forEachItems = new Queue<string>(tsvOptions.GetOrEmpty(forEachTsvFile).Split('\n', '\r').Where(x => x.Trim().Length > 0));
                BlockValues(tsvOptions, queue, forEachTsvFile);

                var hasHeader = tsvOptions.GetOrDefault(forEachTsvFile + ".has.header", true);
                BlockValues(tsvOptions, queue, forEachTsvFile + ".has.header");

                var skipHeader = tsvOptions.GetOrDefault(forEachTsvFile + ".skip.header", true);
                BlockValues(tsvOptions, queue, forEachTsvFile + ".skip.header");

                var defaultColumns = hasHeader ? forEachItems.Dequeue() : "audio.input.id\taudio.input.file";
                var tsvColumns = tsvOptions.GetOrDefault(forEachTsvFile + ".columns", defaultColumns)!;
                BlockValues(tsvOptions, queue, forEachTsvFile + ".columns");

                if (forEachItems.Count() >= 1)
                {
                    queue = ExpandQueueForEachTsvRow(queue, forEachItems, tsvColumns);
                }
            }

            return queue;
        }

        protected static Queue<ICommandValues> ExpandQueueForEachTsvRow(Queue<ICommandValues> queue, Queue<string> tsvRows, string tsvColumns)
        {
            var newQueue = new Queue<ICommandValues>();
            foreach (var row in tsvRows)
            {
                foreach (var item in queue)
                {
                    var values = CombineValuesWithTsvRow(item, tsvColumns, row);
                    newQueue.Enqueue(values);
                }
            }
            return newQueue;
        }

        protected static ICommandValues CombineValuesWithTsvRow(ICommandValues allRows, string tsvColumns, string tsvRow)
        {
            var thisRow = new CommandValues(allRows);
            var tokens = new TsvRowTokenSource(tsvColumns, tsvRow);
            if (!CommandParseDispatcher.ParseCommandValues(tokens, thisRow))
            {
                throw new Exception(thisRow["error"]);
            }

            var names = new List<string>(thisRow.Names);
            foreach (var name in names)
            {
                var value = thisRow[name]!;
                if (value.StartsWith("@@"))
                {
                    var newValue = FileHelpers.ExpandAtFileValue(value.Substring(1), thisRow);
                    thisRow.Reset(name, newValue);
                }
                else if (value.StartsWith("@"))
                {
                    var newValue = FileHelpers.ExpandAtFileValue(value, thisRow);
                    thisRow.Reset(name, newValue);
                }
                else if (value.Contains('{'))
                {
                    var newValue = value.ReplaceValues(thisRow);
                    thisRow.Reset(name, newValue);
                }
            }

            return thisRow;
        }

        protected static ICommandValues CombineValuesWithNameValue(ICommandValues baseValues, string name, string value)
        {
            var values = new CommandValues(baseValues);
            var tokens = new IniLineTokenSource($"{name}={value}");

            if (!CommandParseDispatcher.ParseCommandValues(tokens, values))
            {
                throw new Exception(values["error"]);
            }

            return values;
        }

        protected static bool CheckParallelRunCommand(ICommandValues values, Queue<ICommandValues>? queue, bool expandOk, ref bool parallelOk)
        {
            bool passed;
            var processCount = values.GetOrDefault("x.command.parallel.process.count", 0);
            var threadCount = values.GetOrDefault("x.command.parallel.thread.count", 0);

            if (processCount > 0)
            {
                var rampEvery = values.GetOrDefault("x.command.parallel.ramp.threads.every", 0);
                BlockValues(values, queue!, "x.command.parallel.ramp.processes.every");
                BlockValues(values, queue!, "x.command.parallel.process.count");
                passed = CheckExpectedRunOutOfProcAsync(values, queue!, processCount, rampEvery).Result;
            }
            else if (threadCount > 0)
            {
                var rampEvery = values.GetOrDefault("x.command.parallel.ramp.threads.every", 0);
                BlockValues(values, queue!, "x.command.parallel.ramp.threads.every");
                BlockValues(values, queue!, "x.command.parallel.thread.count");
                passed = CheckExpectedRunOnThreadsAsync(values, queue!, threadCount, rampEvery).Result;
            }
            else
            {
                parallelOk = false;
                passed = CommandRunDispatcher.DispatchRunCommand(values, queue, expandOk, parallelOk);
            }

            return passed;
        }

        protected void CheckPath()
        {
            FileHelpers.UpdatePaths(_values);
        }

        protected string DownloadInputFile(string file, string fileValueName, string fileValueDisplayName)
        {
            var downloaded = HttpHelpers.DownloadFileWithRetry(file);
            if (downloaded == null)
            {
                _values.AddThrowError("ERROR:", $"Cannot download {fileValueDisplayName} file: \"{file}\"");
            }
            else if (FileHelpers.FileExistsInDataPath(downloaded!, _values))
            {
                file = downloaded!;
                _values.Reset(fileValueName, file);
                _delete.Add(file);
            }
            else
            {
                _values.AddThrowError("ERROR:", $"Cannot find {fileValueDisplayName} file: \"{file}\"");
            }

            return file;
        }

        protected string GetIdFromInputUrl(string url, string idValueName)
        {
            var uri = new Uri(url);
            var path = uri.LocalPath;

            var lastSlash = path.LastIndexOfAny("/\\".ToCharArray());
            var lastPart = lastSlash >= 0 ? path.Substring(lastSlash + 1) : path;

            var lastPartValid = !string.IsNullOrEmpty(lastPart);
            var id = lastPartValid ? lastPart : url;

            _values.Add(idValueName, id);
            return id;
        }

        protected void DisposeAfterStop()
        {
            try
            {
                const int maxTimeToWaitForStop = 5000;
                if (_stopEvent.WaitOne(maxTimeToWaitForStop))
                {
                    _disposeAfterStop.Reverse(); // objects should be disposed in reverse order created
                    foreach (var item in _disposeAfterStop)
                    {
                        item?.Dispose();
                    }
                }
            }

            catch (Exception ex)
            {
                FileHelpers.LogException(_values, ex);
            }
        }

        protected void DeleteTemporaryFiles()
        {
            foreach (var file in _delete)
            {
                File.Delete(file);
            }
            _delete.Clear();
        }

        private static bool ShouldCheckExpected(string expected, string notExpected, out IEnumerable<string> expectedItems, out IEnumerable<string> notExpectedItems)
        {
            expectedItems = !string.IsNullOrEmpty(expected)
                ? expected.Split(new char[] { ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                : Enumerable.Empty<string>();
            notExpectedItems = !string.IsNullOrEmpty(notExpected)
                ? notExpected.Split(new char[] { ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                : Enumerable.Empty<string>();
            return expectedItems.Count() + notExpectedItems.Count() > 0;
        }

        private static async Task<bool> CheckExpectedLogOutputAsync(IEnumerable<string> expected, IEnumerable<string> notExpected, bool autoExpect, string autoExpectLogFilter, AutoResetEvent signalStop)
        {
            // if we're supposed to filter the log, do it
            Program.EventLoggerHelpers.SetFilters(autoExpectLogFilter);

            // start writing log lines into the TextWriterReadLineHelper
            var readLineHelper = new TextWriterReadLineHelper();
            var readLineHandler = new EventHandler<string>((sender, message) => readLineHelper.WriteLine(message.TrimEnd('\r', '\n')));
            Program.EventLoggerHelpers.OnMessage += readLineHandler;

            // start checking expectations, using all the lines asychronously from the TextWriterReadLineHelper
            var lines = readLineHelper.ReadAllLinesAsync();
            var checkExpected = ExpectHelper.CheckExpectedLinesAsync(lines, expected, notExpected, autoExpect, false);

            // wait for the stop signal, and then stop receiving log lines
            await Task.Run(() => signalStop.WaitOne());
            Program.EventLoggerHelpers.OnMessage -= readLineHandler;

            // signal the TextWriterReadLineHelper to not wait for more data, once it's finished with the data it has
            // this must happen after unhooking the log message callback (above) (can't write log lines to closed helper)
            readLineHelper.Close();

            // finish checking the lines, if we're not already finished
            return await checkExpected;
        }

        private static async Task<bool> CheckExpectedOutputAsync(ICommandValues values, Queue<ICommandValues> queue, Func<bool> func)
        {
            var expected = values.GetOrEmpty("x.command.output.expect.regex");
            var notExpected = values.GetOrEmpty("x.command.output.not.expect.regex");
            var autoExpect = values.GetOrDefault("x.command.output.auto.expect.regex", false);
            var expectedLog = values.GetOrEmpty("x.command.diagnostics.log.expect.regex");
            var notExpectedLog = values.GetOrEmpty("x.command.diagnostics.log.not.expect.regex");
            var autoExpectLog = values.GetOrDefault("x.command.diagnostics.log.auto.expect.regex", false);
            var autoExpectLogFilter = values.GetOrEmpty("x.command.diagnostics.log.auto.expect.regex.filter");
            var ignoreCheckFailures = values.GetOrDefault("x.command.output.ignore.check.failures", false);

            BlockValues(values, queue, "x.command.output.expect.regex");
            BlockValues(values, queue, "x.command.output.not.expect.regex");
            BlockValues(values, queue, "x.command.output.auto.expect.regex");
            BlockValues(values, queue, "x.command.diagnostics.log.expect.regex");
            BlockValues(values, queue, "x.command.diagnostics.log.not.expect.regex");
            BlockValues(values, queue, "x.command.diagnostics.log.auto.expect.regex");
            BlockValues(values, queue, "x.command.diagnostics.log.auto.expect.regex.filter");

            // if we're supposed to check the log, start checking the log (this must happen before we start checking the function console output)
            var signalStop = new AutoResetEvent(false);
            var checkingLog = ShouldCheckExpected(expectedLog, notExpectedLog, out IEnumerable<string> expectedLogItems, out IEnumerable<string> notExpectedLogItems);
            var taskCheckLog = checkingLog || autoExpectLog
                ? CheckExpectedLogOutputAsync(expectedLogItems, notExpectedLogItems, autoExpectLog, autoExpectLogFilter, signalStop)
                : Task.FromResult<bool>(false);

            // then ... if we're supposed to check the console output, or, auto expect the output, do that, otherwise just run the function
            var checkingConsole = ShouldCheckExpected(expected, notExpected, out IEnumerable<string> expectedConsoleItems, out IEnumerable<string> notExpectedConsoleItems);
            var taskCheckFunc = checkingConsole || autoExpect
                ? ExpectHelper.CheckExpectedConsoleOutputAsync(func, expectedConsoleItems, notExpectedConsoleItems, autoExpect, !autoExpect, ignoreCheckFailures)
                : Task.FromResult<bool>(func());

            // after the function has completed, we can signal the log checking to stop, and wait for it to finish
            await taskCheckFunc;
            signalStop.Set(); // this signal will stop log lines from queuing up to be checked... all lines already queued before this signal will still be checked
            await taskCheckLog;

            // check our results
            var logResult = !checkingLog || taskCheckLog.Result;
            return logResult && taskCheckFunc.Result;
        }

        private static async Task<bool> CheckExpectedRunInProcAsync(ICommandValues values, Queue<ICommandValues> queue)
        {
            return await CheckExpectedOutputAsync(values, queue, () => RunCommandsInProc(queue));
        }

        private static async Task<bool> CheckExpectedRunOutOfProcAsync(ICommandValues values, Queue<ICommandValues> queue, int maxProcesses, int rampEvery)
        {
            return await CheckExpectedOutputAsync(values, queue, () => RunCommandsOutOfProc(queue, maxProcesses, rampEvery));
        }

        private static async Task<bool> CheckExpectedRunOnThreadsAsync(ICommandValues values, Queue<ICommandValues> queue, int maxThreads, int rampEvery)
        {
            return await CheckExpectedOutputAsync(values, queue, () => RunCommandsOnThreads(queue, maxThreads, rampEvery));
        }

        private static bool RunCommandsInProc(Queue<ICommandValues> queue)
        {
            var allPassed = true;
            foreach (var item in queue)
            {
                var passed = CommandRunDispatcher.DispatchRunCommand(item, null, false, false);
                if (!passed) allPassed = false;
            }
            return allPassed;
        }

        private static bool RunCommandsOutOfProc(Queue<ICommandValues> queue, int maxProcesses, int rampEvery)
        {
            Dictionary<string, Process> processes = new Dictionary<string, Process>();

            var rampStarted = DateTime.Now;
            var rampDuration = rampEvery > 0 ? maxProcesses * rampEvery - rampEvery : 0;

            var passed = false;
            while (queue.Count > 0)
            {
                var values = queue.Dequeue();
                var fileNames = values.SaveAs(values.Names, Path.GetTempFileName());
                var fileName = fileNames.Split(';').First();

                var programExe = OperatingSystem.IsWindows() ? Program.Exe : Program.Exe.Replace(".exe", "");
                var start = new ProcessStartInfo(programExe, $"{values.GetCommand()} --nodefaults @{fileName}");
                start.UseShellExecute = false;

                var process = Process.Start(start)!;
                processes.Add(fileNames, process);

                WaitForProcesses(rampStarted, rampDuration, maxProcesses, processes, ref passed);
            }

            WaitForProcesses(null, 0, 0, processes, ref passed);

            return passed;
        }

        private static bool RunCommandsOnThreads(Queue<ICommandValues> queue, int maxThreads, int rampEvery)
        {
            InitMaxThreads(maxThreads);
            List<Thread> threads = new List<Thread>();

            var rampStarted = DateTime.Now;
            var rampDuration = rampEvery > 0 ? maxThreads * rampEvery - rampEvery : 0;

            var passed = true;
            while (queue.Count > 0)
            {
                var values = queue.Dequeue();
                var thread = new Thread(new ThreadStart(() =>
                {
                    if (!CommandRunDispatcher.DispatchRunCommand(values, null, false, false))
                    {
                        passed = false;
                    }
                }));

                threads.Add(thread);
                thread.Start();

                WaitForThreads(rampStarted, rampDuration, maxThreads, threads);
            }

            WaitForThreads(null, 0, 0, threads);

            return passed;
        }

        private static void InitMaxThreads(int maxThreads)
        {
            int max1, max2;
            ThreadPool.GetMaxThreads(out max1, out max2);
            ThreadPool.SetMaxThreads(Math.Max(maxThreads, max1), Math.Max(maxThreads, max2));

            int min1, min2;
            ThreadPool.GetMinThreads(out min1, out min2);
            ThreadPool.SetMinThreads(Math.Min(maxThreads, min1), Math.Max(maxThreads, min2));
        }

        private static void WaitForProcesses(DateTime? rampStarted, int rampDuration, int maxProcesses, Dictionary<string, Process> processes, ref bool exitCodesAllZero)
        {
            exitCodesAllZero = true;

            var value = 1;
            while (processes.Count > 0)
            {
                value = UpdateRampValue(value, rampStarted, rampDuration, maxProcesses);
                if (processes.Count < value) break;

                var items = processes.Where(x => x.Value.HasExited).ToList();
                foreach (var item in items)
                {
                    var fileNames = item.Key;
                    foreach (var fileName in fileNames.Split(';'))
                    {
                        File.Delete(fileName);
                    }

                    processes.Remove(item.Key);

                    if (item.Value.ExitCode != 0)
                    {
                        exitCodesAllZero = false;
                    }
                }

                if (items.Count() == 0)
                {
                    processes.First().Value.WaitForExit(100);
                }
            }
        }

        private static void WaitForThreads(DateTime? rampStarted, int rampDuration, int maxThreads, List<Thread> threads)
        {
            var value = 1;
            while (threads.Count > 0)
            {
                value = UpdateRampValue(value, rampStarted, rampDuration, maxThreads);
                if (threads.Count < value) break;

                var items = threads.Where(x => (x.ThreadState.HasFlag(System.Threading.ThreadState.Stopped))).ToList();
                foreach (var item in items)
                {
                    threads.Remove(item);
                }

                if (items.Count() == 0)
                {
                    threads.First().Join(10);
                }
            }
        }

        private static int UpdateRampValue(int value, DateTime? rampStarted, int rampDuration, int max)
        {
            if (value < max && rampStarted.HasValue && rampDuration > 0)
            {
                value = (int)(DateTime.Now.Subtract(rampStarted.Value).TotalMilliseconds * max / rampDuration);
                if (value > max) value = max;
                if (value < 1) value = 1;
            }
            else if (value < max && rampDuration <= 0)
            {
                value = max;
            }

            return value;
        }

        private static IEnumerable<string> SplitLines(string files)
        {
            return files.Split('\r', '\n').Where(x => x.Trim().Length > 0);
        }

        protected ICommandValues _values;
        protected ManualResetEvent _stopEvent = new ManualResetEvent(false);
        protected ManualResetEvent _canceledEvent = new ManualResetEvent(false);

        protected List<IDisposable?> _disposeAfterStop = new List<IDisposable?>();
        protected List<string> _delete = new List<string>();
    }
}
