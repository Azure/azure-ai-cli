//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Collections;
using System.Threading.Tasks;
using System.Threading;

namespace Azure.AI.Details.Common.CLI
{
    public static class StreamReaderExtensions
    {
        public static async IAsyncEnumerable<string> ReadAllLinesAsync(this StreamReader reader)
        {
            while (true)
            {
                var line = await reader.ReadLineAsync();
                if (line == null)
                {
                    yield break;
                }
                yield return line;
            }
        }
    }
}
