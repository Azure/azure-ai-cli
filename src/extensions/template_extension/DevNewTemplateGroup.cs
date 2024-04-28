//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI.Extensions.Templates
{
    public class DevNewTemplateGroup
    {
        public string LongName { get; set; } = string.Empty;
        public string ShortName { get; set; } = String.Empty;
        public string Languages { get { return string.Join(", ", Items.OrderBy(x => x.Language).Select(x => x.Language)); } }
        public List<DevNewTemplateItem> Items { get; set; } = new List<DevNewTemplateItem>();
    }
}