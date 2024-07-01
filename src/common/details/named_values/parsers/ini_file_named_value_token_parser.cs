//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Azure.AI.Details.Common.CLI
{
    public class IniFileNamedValueTokenParser : NamedValueTokenParser
    {
        // This class represents a named value token parser for an INI file that all commands has to implement.
        //
        // Usage from code:
        //
        //     new IniFileNamedValueTokenParser()
        //
        // Usage from CLI:
        //
        //     @FILE
        //     --ini @FILE
        //
        public IniFileNamedValueTokenParser() :
            base("--ini", "ini.file", "10", "1", "@")
        {
        }
    }
}
