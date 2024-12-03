//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Diagnostics;
using Microsoft.CognitiveServices.Speech.Diagnostics.Logging;

namespace Azure.AI.Details.Common.CLI
{
    public class LogHelpers
    {
        public static void EnsureStartLogFile(ICommandValues values)
        {
            var log = values["diagnostics.config.log.file"];
            if (!string.IsNullOrEmpty(log))
            {
                try
                {
                    log = FileHelpers.GetOutputDataFileName(log, values);
                    FileLogger.Start(log);
                }
                catch
                {
                    ConsoleHelpers.WriteError($"WARNING: Cannot create log file: '{log}'\n\n");
                }
            }
        }

        public static void EnsureStopLogFile(ICommandValues values)
        {
            var log = values["diagnostics.config.log.file"];
            if (!string.IsNullOrEmpty(log))
            {
                FileLogger.Stop();
            }
        }
    }
}
