//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Azure.AI.Details.Common.CLI
{
    public abstract class CLIExecutable
    {
        protected abstract CLIInfo CLIInfo { get; }
        protected abstract IServiceProvider InitializeServices();
        protected abstract bool DisplayKnownErrors(ICommandValues values, Exception ex);
        protected abstract bool DispatchRunCommand(ICommandValues values);
        protected abstract bool DispatchParseCommand(INamedValueTokens tokens, ICommandValues values);
        protected abstract bool DispatchParseCommandValues(INamedValueTokens tokens, ICommandValues values);


        private static CLIExecutable? Instance { get; set; }

        public int Main(string[] mainArgs)
        {
            /* Initialize context with info from the concrete implementation */
            CLIContext.Info = CLIInfo;

            /* Initialize the singleton instance */
            /* TODO: Probably we can keep this in the service provider */
            Instance = this;

            /* Initialize the services */
            CLIContext.ServiceProvider = InitializeServices();

            Console.OutputEncoding = Encoding.UTF8;
            Console.CancelKeyPress += (s, e) =>
            {
                ConsoleGui.Screen.Current.SetCursorVisible(true);
                ConsoleGui.Screen.Current.ResetColors();
                Console.WriteLine("<ctrl-c> received... terminating ... ");
                Process.GetCurrentProcess().Kill();
            };

            var tryPythonRunner = mainArgs.Any(x => x == "python");
            if (tryPythonRunner)
            {
                DisplayBanner(new CommandValues());

                var path = FileHelpers.FindFileInHelpPath($"help/include.python.script.temp.py");
                var script = FileHelpers.ReadAllHelpText(path, Encoding.UTF8);

                (var exit, var output)= PythonRunner.RunScriptAsync(script, "").Result;
                if (exit == 0)
                {
                    Console.WriteLine(output);
                }
                else
                {
                    ConsoleHelpers.WriteLineError("\nERROR: Python script failed!\n");
                    Console.WriteLine("  " + output.Trim().Replace("\n", "\n  "));
                }

                return exit;
            }

            ICommandValues values = new CommandValues();
            INamedValueTokens tokens = new CmdLineTokenSource(mainArgs, values);

            var exitCode = ParseCommand(tokens, values);
            if (exitCode == 0 && !values.DisplayHelpRequested())
            {
                DisplayBanner(values);
                DisplayParsedValues(values);
                exitCode = RunCommand(values) ? 0 : -1;
            }

            if (values.GetOrDefault("x.pause", false))
            {
                Console.Write("Press ENTER to exit... ");
                Console.ReadLine();
            }

            var dumpArgs = string.Join(" ", mainArgs);
            DebugDumpCommandLineArgs(dumpArgs);

            if (OS.IsLinux()) Console.WriteLine();

            AI.DBG_TRACE_INFO($"Command line was: {dumpArgs}");
            AI.DBG_TRACE_INFO($"Exit code: {exitCode}");
            return exitCode;
        }

        internal static void DebugDumpResources()
        {
            var assembly = CLIContext.Info.AssemblyData.BindingAssemblySdkType.Assembly;
            var names = assembly.GetManifestResourceNames();

            Console.WriteLine($"\nDEBUG: {names.Count()} resources found!\n");
            names.ToList().ForEach(x => Console.WriteLine($"  {x}"));
            Console.WriteLine();
        }

        private static void DebugDumpCommandLineArgs(string args)
        {
            if (CLIContext.Debug)
            {
                Console.WriteLine("\nDEBUG: Command line was: {0}", args);
                //Console.WriteLine($"LOCATION: {typeof(Program).Assembly.Location}");
                Console.WriteLine($"LOCATION: {AppContext.BaseDirectory}");
            }
        }

        private static int ParseCommand(INamedValueTokens tokens, ICommandValues values)
        {
            try
            {
                bool parsed = CommandParseDispatcher.DispatchParseCommand(tokens, values);
                return DisplayParseErrorHelpOrException(tokens, values, null, true);
            }
            catch (Exception ex)
            {
                return DisplayParseErrorHelpOrException(tokens, values, ex, true);
            }
        }

        internal static string GetDisplayBannerText()
        {
            string version = GetVersionFromAssembly();
            version = string.IsNullOrEmpty(version) ? "" : $", Version {version}";

            return $"{CLIContext.Name.ToUpper()} - {CLIContext.DisplayName}" + version;
        }

        private static void DisplayBanner(ICommandValues values)
        {
            if (values.GetOrDefault("x.quiet", false)) return;
            if (values.GetOrDefault("x.cls", false)) Console.Clear();

            Console.WriteLine(GetDisplayBannerText());
            Console.WriteLine("Copyright (c) 2023 Microsoft Corporation. All Rights Reserved.");
            Console.WriteLine("");

            var warning = CLIContext.WarningBanner;
            if (!string.IsNullOrEmpty(warning))
            {
                ConsoleHelpers.WriteLineWithHighlight(warning);
                Console.WriteLine("");
            }
        }

        private static string GetVersionFromAssembly()
        {
            var sdkAssembly = CLIContext.Info.AssemblyData.BindingAssemblySdkType?.Assembly;
            var sdkVersionAttribute = sdkAssembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            var thisVersionAttribute = typeof(CLIExecutable).Assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            return sdkVersionAttribute?.InformationalVersion ??
                thisVersionAttribute?.InformationalVersion;
        }

        private static void DisplayCommandHelp(INamedValueTokens tokens, INamedValues values)
        {
            HelpCommandParser.DisplayHelp(tokens, values);
        }

        private static void DisplayParsedValues(INamedValues values)
        {
            if (values.GetOrDefault("x.quiet", true)) return;
            if (!values.GetOrDefault("x.verbose", true)) return;

            const string delimeters = "\r\n";
            const string subKeyPostfix = ".key";
            const string passwordPostfix = ".password";
            const int maxValueLength = 100;
            const int validSubKeyLength = 32;

            var displayed = 0;
            var keys = new SortedSet<string>(values.Names);
            foreach (var key in keys)
            {
                if (key == "error") continue;
                if (key == "display.help") continue;

                var value = values[key];
                var obfuscateValue = key.EndsWith(passwordPostfix) ||
                    (key.Length > subKeyPostfix.Length && key.EndsWith(subKeyPostfix) &&
                     ((value.Length == validSubKeyLength && Guid.TryParse(value, out Guid result)) ||
                      key.Contains("embedded"))); // embedded speech model key can be any length
                if (obfuscateValue)
                {
                    value = $" {value.Substring(0, 4)}****************************";
                }

                var lines = value.Split(delimeters.ToArray()).Where(x => x.Trim().Length > 0);
                int chars = value.Length - maxValueLength;
                value = lines.FirstOrDefault();

                var truncated = "";
                if (lines.Count() > 1)
                {
                    truncated = $" (+{lines.Count() - 1} line(s))";
                    value = "\"" + value.Substring(0, Math.Min(value.Length, maxValueLength)) + "...\"";
                }
                else if (chars > 0)
                {
                    truncated = $" (+{chars} char(s))";
                    value = "\"" + value.Substring(0, Math.Min(value.Length, maxValueLength)) + "...\"";
                }

                Console.WriteLine($"  {key}={value}{truncated}");
                displayed++;
            }

            if (displayed > 0)
            {
                Console.WriteLine();
            }
        }

        private static void DisplayParseError(INamedValueTokens tokens, INamedValues values)
        {
            ConsoleHelpers.WriteLineError("ERROR: Parsing command line!!");
            Console.WriteLine("");

            DisplayParsedValues(values);
            DisplayErrorValue(values);

            if (values.DisplayHelpRequested())
            {
                DisplayCommandHelp(tokens, values);
            }
        }

        private static bool DisplayErrorValue(INamedValues values)
        {
            var error = values["error"];
            if (!string.IsNullOrEmpty(error))
            {
                ConsoleHelpers.WriteLineError($"  {error.Replace("\n", "\n  ")}");
                Console.WriteLine();
                return true;
            }
            return false;
        }

        private static int DisplayParseErrorHelpOrException(INamedValueTokens tokens, ICommandValues values, Exception ex, bool displayBanner)
        {
            if (values.Contains("error"))
            {
                if (displayBanner) DisplayBanner(values);
                DisplayParseError(tokens, values);
                return -2;
            }
            else if (values.DisplayHelpRequested())
            {
                if (displayBanner) DisplayBanner(values);
                DisplayCommandHelp(tokens, values);
                return values.GetOrDefault("display.help.exit.code", 0);
            }
            else if (ex != null)
            {
                if (displayBanner) DisplayBanner(values);
                DisplayLogException(values, ex);
                return -1;
            }

            return 0;
        }

        private static void DisplayException(Exception ex)
        {
            ConsoleHelpers.WriteLineError($"  ERROR: {ex.Message}\n");
        }

        private static void DisplayErrorOrException(ICommandValues values, Exception ex)
        {
            if (ex.InnerException != null)
            {
                DisplayErrorOrException(values, ex.InnerException);
            }
            else if (!DisplayErrorValue(values) && !Instance.DisplayKnownErrors(values, ex))
            {
                DisplayLogException(values, ex);
            }
        }

        private static void DisplayLogException(ICommandValues values, Exception ex)
        {
            FileHelpers.LogException(values, ex);
            DisplayException(ex);
        }

        private static void TryCatchErrorOrException(ICommandValues values, Action action)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                action();
            }
            else
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    DisplayErrorOrException(values, ex);
                }
            }
        }

        private static bool RunCommand(ICommandValues values)
        {
            bool passed = false;

            TryCatchErrorOrException(values, () =>
            {
                passed = CommandRunDispatcher.DispatchRunCommand(values);
            });

            return passed;
        }

        public static bool DelegateDispatchRunCommand(ICommandValues values)
        {
            return Instance.DispatchRunCommand(values);
        }

        public static bool DelegateDispatchParseCommand(INamedValueTokens tokens, ICommandValues values)
        {
            return Instance.DispatchParseCommand(tokens, values);
        }

        public static bool DelegateDispatchParseCommandValues(INamedValueTokens tokens, ICommandValues values)
        {
            return Instance.DispatchParseCommandValues(tokens, values);
        }
    }
}
