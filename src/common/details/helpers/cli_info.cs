//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI;

public readonly record struct CLIInfo(string Name, string DisplayName)
{
    public readonly record struct NameDataRecord(string Name, string DisplayName, string WarningBanner, string TelemetryUserAgent);
    public readonly record struct AssemblyDataRecord(string Exe, string Dll, Type BindingAssemblySdkType);
    public readonly record struct InitCommandDataRecord(bool InitConfigsEndpoint, bool InitConfigsSubscription, string CognitiveServiceResourceKind, string CognitiveServiceResourceSku, string ServiceResourceDisplayNameAllCaps);
    public readonly record struct HelpCommandDataRecord(string HelpCommandTokens);
    public readonly record struct ConfigCommandDataRecord(string ConfigScopeTokens);
    public readonly record struct ZipOptionDataRecord(string[] ZipIncludes);

    public required NameDataRecord NameData { get; init; }

    public required AssemblyDataRecord AssemblyData { get; init; }

    public required InitCommandDataRecord InitCommandData { get; init; }

    public required HelpCommandDataRecord HelpCommandData { get; init; }

    public required ConfigCommandDataRecord ConfigCommandData { get; init; }

    public required ZipOptionDataRecord ZipOptionData { get; init; }
}