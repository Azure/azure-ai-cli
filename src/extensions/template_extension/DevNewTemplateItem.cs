//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI.Extensions.Templates
{
    public class DevNewTemplateItem
    {
        public string LongName { get; set; } = string.Empty;
        public string ShortName { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string UniqueName { get; set; } = string.Empty;
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();
    }
}