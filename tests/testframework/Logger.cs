using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.AI.Details.Common.CLI.TestFramework
{
    public class Logger
    {
        public static void Log(IMessageLogger logger)
        {
            Logger.logger = logger;
        }

        public static void Log(string text)
        {
            var dt = $"{DateTime.Now}";
            using (var mutex = new Mutex(false, "Logger Mutex"))
            {
                mutex.WaitOne();
    
#if DEBUG
                logger?.SendMessage(TestMessageLevel.Informational, $"{dt}: {text}");
#endif
                File.AppendAllText(_logPath, $"{dt}: INFO: {text}\n");
    
                mutex.ReleaseMutex();
            }
        }

        public static void LogIf(bool log, string text)
        {
            if (log) Log(text);
        }

        #region log methods

        public static void LogInfo(string text)
        {
            var dt = $"{DateTime.Now}";
            using (var mutex = new Mutex(false, "Logger Mutex"))
            {
                mutex.WaitOne();
    
                logger?.SendMessage(TestMessageLevel.Informational, $"{dt}: {text}");
                File.AppendAllText(_logPath, $"{dt}: INFO: {text}\n");
    
                mutex.ReleaseMutex();
            }
        }

        public static void LogWarning(string text)
        {
            var dt = $"{DateTime.Now}";
            using (var mutex = new Mutex(false, "Logger Mutex"))
            {
                mutex.WaitOne();    

                logger?.SendMessage(TestMessageLevel.Warning, $"{dt}: {text}");
                File.AppendAllText(_logPath, $"{dt}: WARNING: {text}\n");

                mutex.ReleaseMutex();
            }
        }

        public static void LogError(string text)
        {
            var dt = $"{DateTime.Now}";
            using (var mutex = new Mutex(false, "Logger Mutex"))
            {
                mutex.WaitOne();

                logger?.SendMessage(TestMessageLevel.Error, $"{dt}: {text}");
                File.AppendAllText(_logPath, $"{dt}: ERROR: {text}\n");

                mutex.ReleaseMutex();
            }
        }

        #endregion

        #region private methods and data

        private static string GetLogPath()
        {
            var pid = Process.GetCurrentProcess().Id.ToString();
            var time = DateTime.Now.ToFileTime().ToString();
            return $"log-ai-cli-test-framework-{time}-{pid}.log";
        }
 
        private static IMessageLogger logger = null;

        private static string _logPath = GetLogPath();

        #endregion
    }
}
