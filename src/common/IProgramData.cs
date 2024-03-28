//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using Azure.AI.Details.Common.CLI.Telemetry;

namespace Azure.AI.Details.Common.CLI
{
    public interface IProgramData
    {
        #region name data
        string Name { get; }
        string DisplayName { get; }
        string WarningBanner { get; }
        string TelemetryUserAgent { get; }
        #endregion

        #region assembly data
        string Exe { get; }
        string Dll { get; }
        Type ResourceAssemblyType { get; }
        Type BindingAssemblySdkType { get; }
        #endregion

        #region init command data
        string SERVICE_RESOURCE_DISPLAY_NAME_ALL_CAPS { get; }
        string CognitiveServiceResourceKind { get; }
        string CognitiveServiceResourceSku { get; }
        bool InitConfigsEndpoint { get; }
        bool InitConfigsSubscription { get; }
        #endregion

        #region help command data
        string HelpCommandTokens { get; }
        #endregion

        #region config command data
        string ConfigScopeTokens { get; }
        #endregion

        #region zip option data
        string[] ZipIncludes { get; }
        #endregion

        bool DispatchRunCommand(ICommandValues values);
        bool DispatchParseCommand(INamedValueTokens tokens, ICommandValues values);
        bool DispatchParseCommandValues(INamedValueTokens tokens, ICommandValues values);
        bool DisplayKnownErrors(ICommandValues values, Exception ex);

        IEventLoggerHelpers EventLoggerHelpers { get; }

        ITelemetry Telemetry { get; }
    }
}
