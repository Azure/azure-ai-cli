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

    public class AIResourceException : Exception
    {
        public AIResourceException() { }

        public AIResourceException(string message) : base(message) { }

        public AIResourceException(string message, Exception innerException) : base(message, innerException) { }
    }
}
