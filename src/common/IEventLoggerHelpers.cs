//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI;

public interface IEventLoggerHelpers
{
    void SetFilters(string autoExpectLogFilter);

    event EventHandler<string> OnMessage;
}