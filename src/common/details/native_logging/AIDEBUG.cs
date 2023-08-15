using System.Globalization;
using System.Runtime.CompilerServices;

namespace Azure.AI.Details.Common.CLI
{
    public static class AI
    {
        public static void TRACE_INFO(string message, [CallerLineNumber] int line = 0, [CallerMemberName] string? caller = null, [CallerFilePath] string? file = null)
        {
            DiagnosticsInterop.diagnostics_log_trace_string(DiagnosticsInterop.__TRACE_LEVEL_INFO, "AI_TRACE_INFO:", message, file, line);
        }

        public static void TRACE_WARNING(string message, [CallerLineNumber] int line = 0, [CallerMemberName] string? caller = null, [CallerFilePath] string? file = null)
        {
            DiagnosticsInterop.diagnostics_log_trace_string(DiagnosticsInterop.__TRACE_LEVEL_WARNING, "AI_TRACE_WARNING:", message, file, line);
        }

        public static void TRACE_ERROR(string message, [CallerLineNumber] int line = 0, [CallerMemberName] string? caller = null, [CallerFilePath] string? file = null)
        {
            DiagnosticsInterop.diagnostics_log_trace_string(DiagnosticsInterop.__TRACE_LEVEL_ERROR, "AI_TRACE_ERROR:", message, file, line);
        }

        public static void TRACE_VERBOSE(string message, [CallerLineNumber] int line = 0, [CallerMemberName] string? caller = null, [CallerFilePath] string? file = null)
        {
            DiagnosticsInterop.diagnostics_log_trace_string(DiagnosticsInterop.__TRACE_LEVEL_VERBOSE, "AI_TRACE_VERBOSE:", message, file, line);
        }

        public static void DBG_TRACE_INFO(string message, [CallerLineNumber] int line = 0, [CallerMemberName] string? caller = null, [CallerFilePath] string? file = null)
        {
            DiagnosticsInterop.diagnostics_log_trace_string(DiagnosticsInterop.__TRACE_LEVEL_INFO, "AI_DBG_TRACE_INFO:", message, file, line);
            System.Diagnostics.Debug.WriteLine($"AI_DBG_TRACE_INFO: {message}");
        }

        public static void DBG_TRACE_WARNING(string message, [CallerLineNumber] int line = 0, [CallerMemberName] string? caller = null, [CallerFilePath] string? file = null)
        {
            DiagnosticsInterop.diagnostics_log_trace_string(DiagnosticsInterop.__TRACE_LEVEL_WARNING, "AI_DBG_TRACE_WARNING:", message, file, line);
            System.Diagnostics.Debug.WriteLine($"AI_DBG_TRACE_WARNING: {message}");
        }

        public static void DBG_TRACE_ERROR(string message, [CallerLineNumber] int line = 0, [CallerMemberName] string? caller = null, [CallerFilePath] string? file = null)
        {
            DiagnosticsInterop.diagnostics_log_trace_string(DiagnosticsInterop.__TRACE_LEVEL_ERROR, "AI_DBG_TRACE_ERROR:", message, file, line);
            System.Diagnostics.Debug.WriteLine($"AI_DBG_TRACE_ERROR: {message}");
        }

        public static void DBG_TRACE_VERBOSE(string message, [CallerLineNumber] int line = 0, [CallerMemberName] string? caller = null, [CallerFilePath] string? file = null)
        {
            DiagnosticsInterop.diagnostics_log_trace_string(DiagnosticsInterop.__TRACE_LEVEL_VERBOSE, "AI_DBG_TRACE_VERBOSE:", message, file, line);
            System.Diagnostics.Debug.WriteLine($"AI_DBG_TRACE_VERBOSE: {message}");
        }

        public static void TRACE_INFO(string format, object[] args, [CallerLineNumber] int line = 0, [CallerMemberName] string? caller = null, [CallerFilePath] string? file = null)
        {
            var message = string.Format(format, args);
            TRACE_INFO(message, line, caller, file);
        }

        private static void TRACE_WARNING(string format, object[] args, [CallerLineNumber] int line = 0, [CallerMemberName] string? caller = null, [CallerFilePath] string? file = null)
        {
            var message = string.Format(format, args);
            TRACE_WARNING(message, line, caller, file);
        }

        private static void TRACE_ERROR(string format, object[] args, [CallerLineNumber] int line = 0, [CallerMemberName] string? caller = null, [CallerFilePath] string? file = null)
        {
            var message = string.Format(format, args);
            TRACE_ERROR(message, line, caller, file);
        }

        private static void TRACE_VERBOSE(string format, object[] args, [CallerLineNumber] int line = 0, [CallerMemberName] string? caller = null, [CallerFilePath] string? file = null)
        {
            var message = string.Format(format, args);
            TRACE_VERBOSE(message, line, caller, file);
        }

        public static void DBG_TRACE_INFO(string format, object[] args, [CallerLineNumber] int line = 0, [CallerMemberName] string? caller = null, [CallerFilePath] string? file = null)
        {
            var message = string.Format(format, args);
            DBG_TRACE_INFO(message, line, caller, file);
        }

        private static void DBG_TRACE_WARNING(string format, object[] args, [CallerLineNumber] int line = 0, [CallerMemberName] string? caller = null, [CallerFilePath] string? file = null)
        {
            var message = string.Format(format, args);
            DBG_TRACE_WARNING(message, line, caller, file);
        }

        private static void DBG_TRACE_ERROR(string format, object[] args, [CallerLineNumber] int line = 0, [CallerMemberName] string? caller = null, [CallerFilePath] string? file = null)
        {
            var message = string.Format(format, args);
            DBG_TRACE_ERROR(message, line, caller, file);
        }

        private static void DBG_TRACE_VERBOSE(string format, object[] args, [CallerLineNumber] int line = 0, [CallerMemberName] string? caller = null, [CallerFilePath] string? file = null)
        {
            var message = string.Format(format, args);
            DBG_TRACE_VERBOSE(message, line, caller, file);
        }

        private static object[] Args(params object[] args)
        {
            return args;
        }
    }
}