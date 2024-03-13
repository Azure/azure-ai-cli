//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System.Linq;
using Microsoft.CognitiveServices.Speech;
using Newtonsoft.Json.Linq;

namespace Azure.AI.Details.Common.CLI
{
    public class ConnectionHelpers
    {
        public static void SetConnectionMessageProperties(Connection connection, ICommandValues values)
        {
            var uspConfig = values["usp.speech.config"];
            SetConnectionMessageProperty(connection, "speech.config", uspConfig);

            var uspContext = values["usp.speech.context"];
            SetConnectionMessageProperty(connection, "speech.context", uspContext);
        }

        private static void SetConnectionMessageProperty(Connection connection, string msg, string property)
        {
            if (IsJson(property))
            {
                SetConnectionMessagePropertyJson(connection, msg, property);
            }
            else if (!string.IsNullOrEmpty(property))
            {
                SetConnectionMessagePropertyNameValue(connection, msg, property);
            }
        }

        private static void SetConnectionMessagePropertyJson(Connection connection, string msg, string property)
        {
            var properties = JToken.Parse(property).Children()
                .Where(x => x.Type == JTokenType.Property)
                .Select(x => x as JProperty);

            foreach (var item in properties)
            {
                var value = item.Value.Type == JTokenType.String ? $"\"{item.Value}\"" : item.Value.ToString();
                connection.SetMessageProperty(msg, item.Name, value);
            }
        }

        private static void SetConnectionMessagePropertyNameValue(Connection connection, string msg, string property)
        {
            string name, value;
            if (StringHelpers.SplitNameValue(property, out name, out value))
            {
                int ignore;
                value = int.TryParse(value, out ignore) ? value : $"\"{value}\"";
                connection.SetMessageProperty(msg, name, value);
            }
        }

        private static bool IsJson(string json)
        {
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    JToken.Parse(json);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }
    }
}
