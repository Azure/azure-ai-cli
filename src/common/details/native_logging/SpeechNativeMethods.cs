using System;
using System.Runtime.InteropServices;

namespace Azure.AI.Details.Common.CLI
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void LogMessageCallbackFunctionDelegate(IntPtr nativeUtf8);

    /// <summary>
    /// Cognitive Speech SDK native methods
    /// </summary>
    internal static class SpeechNativeMethods
    {
        public const string IOS_NativeDllName = "__Internal";
        public const CallingConvention IOS_NativeCallConvention = CallingConvention.Cdecl;

        public const string OSX_NativeDllName = "libMicrosoft.CognitiveServices.Speech.core.dylib";
        public const CallingConvention OSX_NativeCallConvention = CallingConvention.Cdecl;

        public const string UNIX_NativeDllName = "libMicrosoft.CognitiveServices.Speech.core.so";
        public const CallingConvention UNIX_NativeCallConvention = CallingConvention.Cdecl;

        public const string Android_NativeDllName = "libMicrosoft.CognitiveServices.Speech.core.so";
        public const CallingConvention Android_NativeCallConvention = CallingConvention.Cdecl;

        public const string Windows_NativeDllName = "Microsoft.CognitiveServices.Speech.core.dll";
        public const CallingConvention Windows_NativeCallConvention = CallingConvention.StdCall;

        #if IOS
            public const string NativeDllName = "__Internal";
            public const CallingConvention NativeCallConvention = CallingConvention.Cdecl;
        #elif OSX
            public const string NativeDllName = "libMicrosoft.CognitiveServices.Speech.core.dylib";
            public const CallingConvention NativeCallConvention = CallingConvention.Cdecl;
        #elif UNIX
            public const string NativeDllName = "libMicrosoft.CognitiveServices.Speech.core.so";
            public const CallingConvention NativeCallConvention = CallingConvention.Cdecl;
        #elif Android
            public const string NativeDllName = "libMicrosoft.CognitiveServices.Speech.core.so";
            public const CallingConvention NativeCallConvention = CallingConvention.Cdecl;
        #else
            public const string NativeDllName = "Microsoft.CognitiveServices.Speech.core.dll";
            public const CallingConvention NativeCallConvention = CallingConvention.StdCall;
        #endif

        [DllImport(NativeDllName, CallingConvention = NativeCallConvention)]
        public static extern void diagnostics_log_trace_string(int level, IntPtr title, IntPtr fileName, int lineNumber, IntPtr message);

        [DllImport(Android_NativeDllName, CallingConvention = Android_NativeCallConvention, EntryPoint = "diagnostics_log_trace_string")]
        public static extern void android_diagnostics_log_trace_string(int level, IntPtr title, IntPtr fileName, int lineNumber, IntPtr message);

        [DllImport(UNIX_NativeDllName, CallingConvention = UNIX_NativeCallConvention, EntryPoint = "diagnostics_log_trace_string")]
        public static extern void unix_diagnostics_log_trace_string(int level, IntPtr title, IntPtr fileName, int lineNumber, IntPtr message);
    }
}