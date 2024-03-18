//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;

namespace Azure.AI.Details.Common.CLI
{
    public static class JsonElementHelpers
    {
        public static string GetPropertyStringOrEmpty(this JsonDocument document, string name)
        {
            return document.RootElement.GetPropertyStringOrEmpty(name);
        }

        public static string GetPropertyStringOrEmpty(this JsonElement element, string name)
        {
            if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value))
            {
                return value.ValueKind == JsonValueKind.String
                    ? value.GetString()
                    : value.GetRawText();
            }
            return string.Empty;
        }

        public static string GetPropertyStringOrNull(this JsonDocument document, string name)
        {
            return document.RootElement.GetPropertyStringOrNull(name);
        }
        
        public static string GetPropertyStringOrNull(this JsonElement element, string name)
        {
            if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value))
            {
                return value.ValueKind == JsonValueKind.String
                    ? value.GetString()
                    : value.GetRawText();
            }
            return null;
        }

        public static bool GetPropertyBool(this JsonDocument document, string name, bool defaultValue)
        {
            return document.RootElement.GetPropertyBool(name, defaultValue);
        }

        public static bool GetPropertyBool(this JsonElement element, string name, bool defaultValue)
        {
            if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value))
            {
                return value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False
                    ? value.GetBoolean()
                    : defaultValue;
            }
            return defaultValue;
        }

        public static JsonElement? GetPropertyElementOrNull(this JsonDocument document, string name)
        {
            return document.RootElement.GetPropertyElementOrNull(name);
        }

        public static JsonElement? GetPropertyElementOrNull(this JsonElement element, string name)
        {
            return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value)
                ? value
                : null;
        }

        public static JsonElement.ArrayEnumerator? GetPropertyArrayOrNull(this JsonDocument document, string name)
        {
            return document.RootElement.GetPropertyArrayOrNull(name);
        }

        public static JsonElement.ArrayEnumerator? GetPropertyArrayOrNull(this JsonElement element, string name)
        {
            return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value)
                ? value.EnumerateArray()
                : null;
        }

        public static JsonElement[] GetPropertyArrayOrEmpty(this JsonDocument document, string name)
        {
            return document.RootElement.GetPropertyArrayOrEmpty(name);
        }

        public static JsonElement[] GetPropertyArrayOrEmpty(this JsonElement element, string name)
        {
            return element.GetPropertyArrayOrNull(name)?.ToArray() ?? Array.Empty<JsonElement>();
        }
    }

    public class JsonHelpers
    {
        #region make member

        public static string MakeMember(string name, string json)
        {
            return $"\"{name}\":{json}";
        }

        public static string MakeMemberOrEmpty(string name, string json)
        {
            return !string.IsNullOrEmpty(json)
                ? MakeMember(name, json)
                : "";
        }

        public static string ContinueWithMemberOrEmpty(string name, string json)
        {
            return StringHelpers.PrependOrEmpty(",", MakeMemberOrEmpty(name, json));
        }

        #endregion

        #region make string

        public static string MakeString(string value)
        {
            return $"\"{value}\"";
        }

        public static string MakeString(INamedValues values, string valueName)
        {
            var value = values.GetOrDefault(valueName, "");
            return MakeString(value);
        }

        public static string MakeStringOrEmpty(string value)
        {
            return !string.IsNullOrEmpty(value)
                ? MakeString(value)
                : "";
        }

        public static string MakeStringOrEmpty(INamedValues values, string valueName)
        {
            var value = values.GetOrDefault(valueName, "");
            return MakeStringOrEmpty(value);
        }

        #endregion

        #region make string array

        public static string MakeStringArray(string delimitedValues, string delimiters)
        {
            string csv = MakeNotEmptyStringsCsv(delimitedValues, delimiters);
            return $"[{csv}]";
        }

        public static string MakeStringArray(INamedValues values, string delimitedValuesName, string delimiters)
        {
            var delimitedValues = values.GetOrDefault(delimitedValuesName, "");
            return MakeStringArray(delimitedValues, delimiters);
        }

        public static string MakeStringArray(INamedValues values, string singleValueName, string delimitedValuesName, string delimiters)
        {
            var singleValue = values.GetOrDefault(singleValueName, "");
            var delimitedValues = values.GetOrDefault(delimitedValuesName, "");
            return MakeStringArray($"{singleValue};{delimitedValues}", delimiters);
        }

        public static string MakeStringArrayOrEmpty(string delimitedValues, string delimiters)
        {
            string csv = MakeNotEmptyStringsCsv(delimitedValues, delimiters);
            return !string.IsNullOrEmpty(csv)
                ? $"[{csv}]"
                : "";
        }

        public static string MakeStringArrayOrEmpty(INamedValues values, string delimitedValuesName, string delimiters)
        {
            var delimitedValues = values.GetOrDefault(delimitedValuesName, "");
            return MakeStringArrayOrEmpty(delimitedValues, delimiters);
        }

        public static string MakeStringArrayOrEmpty(INamedValues values, string singleValueName, string delimitedValuesName, string delimiters)
        {
            var singleValue = values.GetOrDefault(singleValueName, "");
            var delimitedValues = values.GetOrDefault(delimitedValuesName, "");

            var singleOk = !string.IsNullOrEmpty(singleValue);
            var delimitedOk = !string.IsNullOrEmpty(delimitedValues);

            delimitedValues = singleOk && delimitedOk
                ? $"{singleValue};{delimitedValues}"
                : delimitedOk
                    ? delimitedValues
                    : singleValue;

            return MakeStringArrayOrEmpty(delimitedValues, delimiters);
        }

        #endregion

        #region make string member

        public static string MakeStringMember(string name, string value)
        {
            return MakeMember(name, MakeString(value));
        }

        public static string MakeStringMember(string name, INamedValues values, string valueName)
        {
            return MakeStringMember(name, MakeString(values, valueName));
        }

        public static string MakeStringMemberOrEmpty(string name, string value)
        {
            return MakeMemberOrEmpty(name, MakeStringOrEmpty(value));
        }

        public static string MakeStringMemberOrEmpty(string name, INamedValues values, string valueName)
        {
            return MakeMemberOrEmpty(name, MakeStringOrEmpty(values, valueName));
        }

        public static string ContinueWithStringMemberOrEmpty(string name, string value)
        {
            return StringHelpers.PrependOrEmpty(",", MakeStringMemberOrEmpty(name, value));
        }

        public static string ContinueWithStringMemberOrEmpty(string name, INamedValues values, string valueName)
        {
            return StringHelpers.PrependOrEmpty(",", MakeStringMemberOrEmpty(name, values, valueName));
        }

        #endregion

        #region make string array member

        public static string MakeStringArrayMember(string name, string delimitedValues, string delimiters)
        {
            return MakeMember(name, MakeStringArray(delimitedValues, delimiters));
        }

        public static string MakeStringArrayMember(string name, INamedValues values, string delimitedValuesName, string delimiters)
        {
            return MakeMember(name, MakeStringArray(values, delimitedValuesName, delimiters));
        }

        public static string MakeStringArrayMember(string name, INamedValues values, string singleValueName, string multipleValueName, string delimiters)
        {
            string jsonArray = MakeStringArray(values, singleValueName, multipleValueName, delimiters);
            return MakeMember(name, jsonArray);
        }

        public static string MakeStringArrayMemberOrEmpty(string name, string delimitedValues, string delimiters)
        {
            return MakeMemberOrEmpty(name, MakeStringArrayOrEmpty(delimitedValues, delimiters));
        }

        public static string MakeStringArrayMemberOrEmpty(string name, INamedValues values, string delimitedValuesName, string delimiters)
        {
            return MakeMemberOrEmpty(name, MakeStringArrayOrEmpty(values, delimitedValuesName, delimiters));
        }

        public static string MakeStringArrayMemberOrEmpty(string name, INamedValues values, string singleValueName, string multipleValueName, string delimiters)
        {
            string jsonArray = MakeStringArrayOrEmpty(values, singleValueName, multipleValueName, delimiters);
            return MakeMemberOrEmpty(name, jsonArray);
        }

        public static string ContinueWithStringArrayMemberOrEmpty(string name, string delimitedValues, string delimiters)
        {
            return StringHelpers.PrependOrEmpty(",", MakeStringArrayMemberOrEmpty(name, delimitedValues, delimiters));
        }

        public static string ContinueWithStringArrayMemberOrEmpty(string name, INamedValues values, string delimitedValuesName, string delimiters)
        {
            return StringHelpers.PrependOrEmpty(",", MakeStringArrayMemberOrEmpty(name, values, delimitedValuesName, delimiters));
        }

        public static string ContinueWithStringArrayMemberOrEmpty(string name, INamedValues values, string singleValueName, string multipleValueName, string delimiters)
        {
            return StringHelpers.PrependOrEmpty(",", MakeStringArrayMemberOrEmpty(name, values, singleValueName, multipleValueName, delimiters));
        }

        #endregion

        public static void PrintJson(string text, string indent = "  ", bool naked = false)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                PrintJson(JsonDocument.Parse(text).RootElement, indent, naked);
            }
        }

        private static void PrintJson(JsonElement element, string indent = "  ", bool naked = false)
        {
            var print = !naked 
                ? element.GetRawText() 
                : element.GetRawText()
                    .Replace("  \"", "  ")
                    .Replace("\": \"", ": ")
                    .Replace("\": ", ": ")
                    .Replace("\",\r", "\r")
                    .Replace(",\r", "\r")
                    .Replace("\"\r", "\r");

            Console.WriteLine(indent + print.Replace("\n", "\n" + indent) + "\n");
        }

        private static string MakeNotEmptyStringsCsv(string delimitedValues, string delimiters)
        {
            var sb = new StringBuilder();
            var array = delimitedValues.Split(delimiters.ToCharArray()).ToList();
            foreach (var item in array)
            {
                if (!string.IsNullOrEmpty(item))
                {
                    sb.Append(MakeString(item));
                    sb.Append(", ");
                }
            }

            return sb.ToString().Trim(',', ' ');
        }

        public static string MergeJsonObjects(List<JsonElement> allPages)
        {
            var properties = new Dictionary<string, string>();
            foreach (var page in allPages)
            {
                foreach (var property in page.EnumerateObject())
                {
                    if (!properties.ContainsKey(property.Name))
                    {
                        properties.Add(property.Name, property.Value.GetRawText());
                    }
                    else if (property.Value.ValueKind == JsonValueKind.Array)
                    {
                        properties[property.Name] = MergeJsonArrays(properties[property.Name], property.Value.GetRawText());
                    }
                    else
                    {
                        properties[property.Name] = property.Value.GetRawText();
                    }
                }
            }

            // return as json object
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions{ Indented = false });
            writer.WriteStartObject();
            foreach (var property in properties)
            {
                writer.WritePropertyName(property.Key);
                writer.WriteRawValue(property.Value);
            }
            writer.WriteEndObject();
            writer.Flush();
            return Encoding.UTF8.GetString(stream.ToArray());
        }

        private static string MergeJsonArrays(string v1, string v2)
        {
            var array1 = JsonDocument.Parse(v1).RootElement.EnumerateArray().ToList();
            var array2 = JsonDocument.Parse(v2).RootElement.EnumerateArray().ToList();
            array1.AddRange(array2);
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions{ Indented = false });
            writer.WriteStartArray();
            foreach (var item in array1)
            {
                item.WriteTo(writer);
            }
            writer.WriteEndArray();
            writer.Flush();
            return Encoding.UTF8.GetString(stream.ToArray());
        }

        public static string GetJsonObjectText(Dictionary<string, List<string>> properties)
        {
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions{ Indented = false });

            WriteJsonObject(writer, properties);

            writer.Flush();
            return Encoding.UTF8.GetString(stream.ToArray());
        }

        public static string GetJsonArrayText(List<Dictionary<string, string>> list)
        {
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions{ Indented = false });

            WriteJsonArray(writer, list);

            writer.Flush();
            return Encoding.UTF8.GetString(stream.ToArray());
        }

        private static void WriteJsonArray(Utf8JsonWriter writer, List<Dictionary<string, string>> items)
        {
            writer.WriteStartArray();
            foreach (var item in items.Where(x => x != null).ToList())
            {
                WriteJsonObject(writer, item);
            }
            writer.WriteEndArray();
        }

        private static void WriteJsonObject(Utf8JsonWriter writer, Dictionary<string, string> properties)
        {
            writer.WriteStartObject();
            foreach (var key in properties.Keys)
            {
                WritePropertyJsonOrString(writer, key, properties[key]);
            }
            writer.WriteEndObject();
        }

        private static void WritePropertyJsonOrString(Utf8JsonWriter writer, string key, string value)
        {
            if (key.EndsWith(".json"))
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    writer.WritePropertyName(key);
                    writer.WriteRawValue(value);
                }
            }
            else
            {
                writer.WriteString(key, value);
            }
        }

        private static void WriteJsonOrStringValue(Utf8JsonWriter writer, string key, string value)
        {
            if (key.EndsWith(".json"))
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    writer.WriteRawValue(value);
                }
            }
            else
            {
                writer.WriteStringValue(value);
            }
        }

        private static void WriteJsonObject(Utf8JsonWriter writer, Dictionary<string, List<string>> properties)
        {
            writer.WriteStartObject();
            foreach (var key in properties.Keys)
            {
                var values = properties[key].Where(x => !string.IsNullOrEmpty(x));
                if (values.Count() == 1)
                {
                    WritePropertyJsonOrString(writer, key, values.First());
                    continue;
                }

                writer.WriteStartArray(key);
                foreach (var item in values)
                {
                    WriteJsonOrStringValue(writer, key, item);
                }
                writer.WriteEndArray();
            }
            writer.WriteEndObject();
        }
    }
}
