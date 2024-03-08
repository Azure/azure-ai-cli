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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Azure.AI.Details.Common.CLI
{
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
                PrintJson(JToken.Parse(text), indent, naked);
            }
        }

        private static void PrintJson(JToken token, string indent = "  ", bool naked = false)
        {
            var print = !naked
                ? token.ToString()
                : token.ToString()
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

    }
}
