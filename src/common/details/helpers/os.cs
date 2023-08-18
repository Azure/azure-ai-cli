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
    }
}