//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System.Collections.Generic;
using System.IO;

namespace Azure.AI.Details.Common.CLI.TestFramework
{
    public struct YamlTestCaseParseContext
    {
        public string Source;
        public FileInfo File;

        public string Area;
        public string Class;
        public Dictionary<string, List<string>> Tags;

        public Dictionary<string, string> Environment;
        public string WorkingDirectory;
    }
}
