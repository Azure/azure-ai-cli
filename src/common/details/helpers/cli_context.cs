//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//
namespace Azure.AI.Details.Common.CLI;

public static class CLIContext
{
    public static bool Debug { get; internal set; }
    public static CLIInfo Info { get; internal set; }

    public static IServiceProvider ServiceProvider { get; internal set; }

    public static string Name => Info.NameData.Name;
    public static string DisplayName => Info.NameData.DisplayName;
    public static string WarningBanner => Info.NameData.WarningBanner;
    public static string TelemetryUserAgent => Info.NameData.TelemetryUserAgent;
    public static string Exe => OperatingSystem.IsWindows() ? Info.AssemblyData.Exe : Info.AssemblyData.Exe.Replace(".exe", "");
    public static string Dll => Info.AssemblyData.Dll;

    public static string[] ZipIncludes => Info.ZipOptionData.ZipIncludes;
}