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
    public partial class Program
    {
        public static bool Debug { get; internal set; }

        public static int Main(string[] mainArgs)
        {
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

                var path = FileHelpers.FindFileInHelpPath($"help/include.python.script.hub_list.py");
                var script = FileHelpers.ReadAllHelpText(path, Encoding.UTF8);

                (var exit, var output)= PythonRunner.RunScriptAsync(script, "--subscription e72e5254-f265-4e95-9bd2-9ee8e7329051").Result;
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
                // Avoid error messages about missing online key/region with embedded
                if (values.Contains("embedded.config.embedded"))
                {
                    values.Reset("service.config.key");
                    values.Reset("service.config.region");
                }
                DisplayParsedValues(values);
                exitCode = RunCommand(values) ? 0 : -1;
            }

            if (values.GetOrDefault("x.pause", false))
            {
                Console.Write("Press ENTER to exit... ");
                Console.ReadLine();
            }

            if (Program.Debug)
            {
                DebugDumpCommandLineArgs(mainArgs);
            }

            return exitCode;
        }

        internal static void DebugDumpResources()
        {
            var assembly = typeof(Program).Assembly;
            var names = assembly.GetManifestResourceNames();

            Console.WriteLine($"\nDEBUG: {names.Count()} resources found!\n");
            names.ToList().ForEach(x => Console.WriteLine($"  {x}"));
            Console.WriteLine();
        }

        private static void DebugDumpCommandLineArgs(string[] mainArgs)
        {
            Console.WriteLine("\nDEBUG: Command line was: {0}", string.Join(" ", mainArgs));
            //Console.WriteLine($"LOCATION: {typeof(Program).Assembly.Location}");
            Console.WriteLine($"LOCATION: {AppContext.BaseDirectory}");
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

            return $"{Program.Name.ToUpper()} - {Program.DisplayName}" + version;
        }

        private static void DisplayBanner(ICommandValues values)
        {
            if (values.GetOrDefault("x.quiet", false)) return;
            if (values.GetOrDefault("x.cls", false)) Console.Clear();

            Console.WriteLine(GetDisplayBannerText());
            Console.WriteLine("Copyright (c) 2022 Microsoft Corporation. All Rights Reserved.");
            Console.WriteLine("");
        }

        private static string GetVersionFromAssembly()
        {
            var sdkAssembly = Program.BindingAssemblySdkType?.Assembly;
            var sdkVersionAttribute = sdkAssembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            var thisVersionAttribute = typeof(Program).Assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
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
            else if (!DisplayErrorValue(values) && !DisplayKnownErrors(values, ex))
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
    }
}
