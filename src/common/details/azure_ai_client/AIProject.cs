//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Azure.AI.Details.Common.CLI
{
    public class AIProject
    {
        public string name { get; set; }
        public string location { get; set; }
        public string id { get; set; }
        public string resrouce_group { get; set; }
        public string display_name { get; set; }
        public string workspace_hub { get; set;}
    }
}
