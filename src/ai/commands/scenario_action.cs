//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;

namespace Azure.AI.Details.Common.CLI
{
    public class ScenarioAction
    {
        public ScenarioAction(string scenario, string action, Action<ScenarioAction> invokeAction) : this(scenario, null, action, invokeAction)
        {
        }

        public ScenarioAction(string scenario, string actionPrefix, string action, Action<ScenarioAction> invokeAction)
        {
            Scenario = scenario;
            InvokeAction = invokeAction;

            Action = action;
            ActionPrefix = actionPrefix;
        }

        public string Scenario { get; set; }

        public string Action { get; set; }
        public string ActionPrefix { get; set; }

        public string ActionWithPrefix => string.IsNullOrEmpty(ActionPrefix) ? Action : $"{ActionPrefix} {Action}";

        Action<ScenarioAction> InvokeAction { get; set; }
    }
}
