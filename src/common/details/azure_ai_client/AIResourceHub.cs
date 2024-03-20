//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using Azure.ResourceManager.MachineLearning.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Azure.AI.Details.Common.CLI
{

    public class AIResourceHub
    {
        public string name { get; set; }
        public string location { get; set; }
        public string id { get; set; }
        public string resource_group { get; set; }
        public string display_name { get; set; }
        private List<string> associated_projects = new List<string>();
        public List<string> projects
        {
            get { return associated_projects; }
        }

        public void AddAssociatedAIProject(string projectId)
        {
            associated_projects.Add(projectId);
        }
    }
}
