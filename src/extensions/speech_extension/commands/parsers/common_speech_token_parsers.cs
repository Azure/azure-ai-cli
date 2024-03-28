//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.AI.Details.Common.CLI
{
    public class SpeechConfigServiceConnectionTokenParser : NamedValueTokenParserList
    {
        public SpeechConfigServiceConnectionTokenParser() : base(

            new NamedValueTokenParser("--host",       "service.config.host", "001", "1"),
            new NamedValueTokenParser("--endpointid", "service.config.endpoint.id", "0011", "1"),
            new NamedValueTokenParser(null,           "service.config.endpoint.query.string", "00011", "1"),
            new NamedValueTokenParser(null,           "service.config.endpoint.http.header", "00011", "1"),
            new NamedValueTokenParser(null,           "service.config.endpoint.traffic.type", "00011", "1"),
            new NamedValueTokenParser("--uri",        "service.config.endpoint.uri", "0010;0001", "1"),

            // new NamedValueTokenParser("--aad",        "service.config.token.aad", "001", "1;0", null, "service.config.token.value", "aad", "service.config.token.type"),
            // new NamedValueTokenParser("--msa",        "service.config.token.msa", "001", "1;0", null, "service.config.token.value", "msa", "service.config.token.type"),
            // new NamedValueTokenParser("--token.type", "service.config.token.type", "011", "1", "msa;aad"),
            // new NamedValueTokenParser("--password",   "service.config.token.password", "001", "1"),
            // new NamedValueTokenParser("--user",       "service.config.token.username", "001", "1"),
            new NamedValueTokenParser("--token.value","service.config.token.value", "0010", "1"),

            new NamedValueTokenParser(null,           "service.config.proxy.port", "0011", "1"),
            new NamedValueTokenParser(null,           "service.config.proxy.host", "0010", "1")

        ) {}
    }

    public class ConnectDisconnectNamedValueTokenParser : NamedValueTokenParserList
    {
        public ConnectDisconnectNamedValueTokenParser() : base(
            new NamedValueTokenParser(null,           "connection.connect", "01", "1", "true;false"),
            new NamedValueTokenParser("--connect",    "connection.connect", "01", "1;0", "true;false", null, "true"),
            new NamedValueTokenParser(null,           "connection.disconnect", "01", "1", "true;false"),
            new NamedValueTokenParser("--disconnect", "connection.disconnect", "01", "1;0", "true;false", null, "true")
        ) {}
    }
}
