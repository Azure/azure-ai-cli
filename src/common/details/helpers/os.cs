//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Runtime.InteropServices;

namespace Azure.AI.Details.Common.CLI
{
    public static class OS
    {
        public static bool IsWindows() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static bool IsMac() => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        public static bool IsLinux() => RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && !IsAndroid();
        public static bool IsAndroid()
        {
            return Environment.GetEnvironmentVariable("ANDROID_ROOT") != null &&
                Environment.GetEnvironmentVariable("ANDROID_DATA") != null;
        }
        public static bool IsCodeSpaces()
        {
            return Environment.GetEnvironmentVariable("CODESPACES") == "true";
        }
    }
}