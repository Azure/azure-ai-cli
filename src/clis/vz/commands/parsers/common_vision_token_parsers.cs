//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.AI.Details.Common.CLI
{
    public class VisionServiceOptionsTokenParser : NamedValueTokenParserList
    {
        public VisionServiceOptionsTokenParser() : base(

            new Any1ValueNamedValueTokenParser("--host", "service.config.host", "001"),
            new Any1ValueNamedValueTokenParser("--endpointid", "service.config.endpoint.id", "0011"),
            new Any1ValueNamedValueTokenParser(null, "service.config.endpoint.query.string", "00011"),
            new Any1ValueNamedValueTokenParser(null, "service.config.endpoint.http.header", "00011"),
            new Any1ValueNamedValueTokenParser(null, "service.config.endpoint.traffic.type", "00011"),
            new Any1ValueNamedValueTokenParser(null, "service.config.endpoint.type", "0011"),
            new Any1ValueNamedValueTokenParser("--uri", "service.config.endpoint.uri", "0010;0001"),

            new Any1ValueNamedValueTokenParser("--token.value", "service.config.token.value", "0010"),

            new Any1ValueNamedValueTokenParser(null, "service.config.proxy.port", "0011"),
            new Any1ValueNamedValueTokenParser(null, "service.config.proxy.host", "0010")

        ) {}
    }
}
