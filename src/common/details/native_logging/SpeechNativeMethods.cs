//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

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
        private const string NativeDllName = "Microsoft.CognitiveServices.Speech.core";

        #if IOS
            private const CallingConvention NativeCallConvention = CallingConvention.Cdecl;
        #elif OSX
            private const CallingConvention NativeCallConvention = CallingConvention.Cdecl;
        #elif UNIX
            private const CallingConvention NativeCallConvention = CallingConvention.Cdecl;
        #elif Android
            private const CallingConvention NativeCallConvention = CallingConvention.Cdecl;
        #else
            private const CallingConvention NativeCallConvention = CallingConvention.StdCall;
        #endif

        [DllImport(NativeDllName, CallingConvention = NativeCallConvention)]
        internal static extern void diagnostics_log_trace_string(int level, IntPtr title, IntPtr fileName, int lineNumber, IntPtr message);
    }
}