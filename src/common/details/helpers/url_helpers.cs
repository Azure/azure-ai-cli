//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.Collections.Generic;
using System.Text.Json;

namespace Azure.AI.Details.Common.CLI
{
    [Flags]
    public enum IdKind
    {
        None = 0,
        Guid = 1,
        FlatDateTime = 2,
        Name = 4,
        Id = 8
    }

    public class IdHelpers
    {
        public static string IdFromString(string value, IdKind kind = IdKind.Guid)
        {
            if ((kind & IdKind.Guid) != 0)
            {
                var match = Regex.Match(value, guidPatternEnd);
                return match.Success ? match.Value : string.Empty;
            }

            if ((kind & IdKind.FlatDateTime) != 0)
            {
                var match = Regex.Match(value, flatDateTimePatternEnd);
                return match.Success ? match.Value : string.Empty;
            }

            if ((kind & IdKind.Name) != 0)
            {
                var match = Regex.Match(value, namePatternEnd);
                return match.Success ? match.Value : string.Empty;
            }

            return string.Empty;
        }

        public static string GetIdFromNamedValue(ICommandValues values, string name, string defaultValue = "")
        {
            var value = values.GetOrDefault(name, defaultValue);
            return IdHelpers.IdFromString(value!);
        }

        public static void CheckWriteOutputNameOrId(string nameOrId, INamedValues values, string domain, IdKind kind)
        {
            var atIdName = kind == IdKind.Name
                ? $"{domain}.output.name"
                : $"{domain}.output.id";
            var atId = values.GetOrEmpty(atIdName);
            var atIdOk = !string.IsNullOrEmpty(atId);
            if (atIdOk)
            {
                var atIdFile = FileHelpers.GetOutputDataFileName(atId, values)!;
                FileHelpers.WriteAllText(atIdFile, nameOrId, Encoding.UTF8);
                values.Reset(atIdName); // once we wrote it, don't try to again
            }

            var addIdName = kind == IdKind.Name
                ? $"{domain}.output.add.name"
                : $"{domain}.output.add.id";
            var addId = values.GetOrEmpty(addIdName);
            var addIdOk = !string.IsNullOrEmpty(addId);
            if (addIdOk)
            {
                var addIdFile = FileHelpers.GetOutputDataFileName(addId, values)!;
                FileHelpers.AppendAllText(addIdFile, "\n" + nameOrId, Encoding.UTF8);
                values.Reset(addIdName); // once we wrote it, don't try to again
            }
        }

        public static void CheckWriteOutputNameOrId(string json, string nameOrIdName, INamedValues values, string domain, IdKind kind)
        {
            var parsed = JsonDocument.Parse(json);
            var value = parsed.GetPropertyElementOrNull(nameOrIdName);
            var nameOrId = value?.GetString();
            if (nameOrId != null)
            {
                CheckWriteOutputNameOrId(nameOrId, values, domain, kind);
            }
        }

        public static void CheckWriteOutputNamesOrIds<T>(IEnumerable<T> items, ICommandValues values, string domain, IdKind kind, Func<T, string> func)
        {
            var outKey = kind == IdKind.Name
                ? $"{domain}.output.names"
                : $"{domain}.output.ids";
            var outVal = values.GetOrEmpty(outKey);
            var outOk = !string.IsNullOrEmpty(outVal);
            if (outOk)
            {
                var atFile = FileHelpers.GetOutputDataFileName(outVal, values)!;
                var namesOrIds = string.Join('\n', items.Select(func));
                FileHelpers.WriteAllText(atFile, namesOrIds, Encoding.UTF8);
                values.Reset(outKey); // once we wrote it, don't try to again
            }

            var addKey = kind == IdKind.Name
                ? $"{domain}.output.add.names"
                : $"{domain}.output.add.ids";
            var addVal = values.GetOrEmpty(addKey);
            var addOk = !string.IsNullOrEmpty(addVal);
            if (addOk)
            {
                var addFile = FileHelpers.GetOutputDataFileName(addVal, values)!;
                var namesOrIds = string.Join('\n', items.Select(func));
                FileHelpers.AppendAllText(addFile, "\n" + namesOrIds, Encoding.UTF8);
                values.Reset(addKey); // once we wrote it, don't try to again
            }
        }

        #region private data
        private static readonly string guidPatternEnd = "([0-9A-Fa-f]{8}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{12})$";
        private static readonly string flatDateTimePatternEnd = "((?#year)20[0-9][0-9])((?#month)(0[1-9])|(1[0-2]))((?#day)(0[1-9])|([12][0-9])|(3[0-1]))((?#time)[0-9]{10})$";
        private static readonly string namePatternEnd = "(?<=[/\\\\])[^/\\\\]+$";

        #endregion
    }

    public class UrlHelpers
    {
        public static string ContinueWithQueryString(string name, string value)
        {
            return $"&{name}={value}";
        }

        public static string ContinueWithQueryString(string name, INamedValues values, string valueName)
        {
            var value = values.GetOrEmpty(valueName);
            return ContinueWithQueryString(name, value);
        }

        public static string ContinueWithQueryStringOrEmpty(string name, string value)
        {
            return !string.IsNullOrEmpty(value)
                ? ContinueWithQueryString(name, value)
                : "";
        }

        public static string ContinueWithQueryStringOrEmpty(string name, INamedValues values, string valueName)
        {
            var value = values.GetOrEmpty(valueName);
            return ContinueWithQueryStringOrEmpty(name, value);
        }

        public static string IdFromUrl(string value, IdKind kinds = IdKind.Guid)
        {
            return IdHelpers.IdFromString(value, kinds);
        }

        public static string CheckWriteOutputUrlOrId(string url, INamedValues values, string domain, IdKind kinds = IdKind.Guid)
        {
            CheckWriteOutputUrl(url, values, domain);
            CheckWriteOutputId(url, values, domain, kinds);
            return url;
        }

        public static string? CheckWriteOutputUrlOrId(string json, string urlName, INamedValues values, string domain, IdKind kinds = IdKind.Guid)
        {
            var parsed = JsonDocument.Parse(json);
            var value = parsed?.GetPropertyElementOrNull(urlName);
            var url = value?.GetString();
            return url != null ? CheckWriteOutputUrlOrId(url!, values, domain, kinds) : null;
        }

        public static void CheckWriteOutputUrlsOrIds(string json, string arrayName, string urlName, INamedValues values, string domain, IdKind kinds = IdKind.Guid)
        {
            var urls = new List<string>();
            var ids = new List<string>();
            GetUrlsOrIds(json, arrayName, urlName, urls, ids, kinds);
            CheckWriteOutputUrlsOrIds(values, domain, urls, ids, kinds);
        }

        public static void CheckWriteOutputUrlsOrIds(INamedValues values, string domain, List<string> urls, List<string> ids, IdKind kind)
        {
            var atUrlsName = $"{domain}.output.urls";
            var atUrls = values.GetOrEmpty(atUrlsName);
            var atUrlsOk = !string.IsNullOrEmpty(atUrls);

            var atIdsName = kind == IdKind.Name
                ? $"{domain}.output.names"
                : $"{domain}.output.ids";
            var atIds = values.GetOrEmpty(atIdsName);
            var atIdsOk = !string.IsNullOrEmpty(atIds);

            if (atUrlsOk)
            {
                var atUrlsFile = FileHelpers.GetOutputDataFileName(atUrls, values)!;
                FileHelpers.WriteAllLines(atUrlsFile, urls, new UTF8Encoding(false));
                values.Reset(atUrlsName); // only write once
            }

            if (atIdsOk)
            {
                var atIdsFile = FileHelpers.GetOutputDataFileName(atIds, values)!;
                FileHelpers.WriteAllLines(atIdsFile, ids, new UTF8Encoding(false));
                values.Reset(atIdsName);
            }

            var outputLast = values.GetOrDefault($"{domain}.output.list.last", false);
            if (outputLast && urls.Count() > 0)
            {
                var url = urls.Last();
                UrlHelpers.CheckWriteOutputUrlOrId(url, values, domain, kind);
            }
        }

        #region private methods

        private static void CheckWriteOutputUrl(string url, INamedValues values, string domain)
        {
            var urlOk = !string.IsNullOrEmpty(url) && url.StartsWith("http");
            if (!urlOk) return;

            var atUrlName = $"{domain}.output.url";
            var atUrl = values.GetOrEmpty(atUrlName);
            var atUrlOk = !string.IsNullOrEmpty(atUrl);
            if (atUrlOk)
            {
                var atUrlFile = FileHelpers.GetOutputDataFileName(atUrl, values)!;
                FileHelpers.WriteAllText(atUrlFile, url, Encoding.UTF8);
                values.Reset(atUrlName); // once we wrote it, don't try to again
            }

            var addUrlName = $"{domain}.output.add.url";
            var addUrl = values.GetOrEmpty(addUrlName);
            var addUrlOk = !string.IsNullOrEmpty(addUrl);
            if (addUrlOk)
            {
                var addUrlFile = FileHelpers.GetOutputDataFileName(addUrl, values)!;
                FileHelpers.AppendAllText(addUrlFile, "\n" + url, Encoding.UTF8);
                values.Reset(addUrlName); // once we wrote it, don't try to again
            }
        }

        private static void CheckWriteOutputId(string url, INamedValues values, string domain, IdKind kinds = IdKind.Guid)
        {
            var urlOk = !string.IsNullOrEmpty(url) && url.StartsWith("http");
            if (!urlOk) return;

            foreach (var kind in Enum.GetValues(typeof(IdKind)).Cast<IdKind>())
            {
                if ((kind & kinds) != 0)
                {
                    var id = IdFromUrl(url, kind);
                    var idOk = !string.IsNullOrEmpty(id);
                    if (!idOk) continue;

                    IdHelpers.CheckWriteOutputNameOrId(id, values, domain, kind);
                }
            }
        }

        public static void GetUrlsOrIds(string json, string arrayName, string urlName, IList<string> urls, IList<string> ids, IdKind kinds)
        {
            var getUrlsOnlyOnce = urls;
            foreach (var thisKind in Enum.GetValues(typeof(IdKind)).Cast<IdKind>())
            {
                var askedForThisKind = (thisKind & kinds) != 0;
                if (askedForThisKind)
                {
                    GetUrlsAndIdsForOneKind(json, arrayName, urlName, getUrlsOnlyOnce!, ids, thisKind);
                    getUrlsOnlyOnce = null; // we'll get the URLs on the first
                }
            }
        }

        private static void GetUrlsAndIdsForOneKind(string json, string arrayName, string urlName, IList<string> urls, IList<string> ids, IdKind oneKind)
        {
            var parsed = JsonDocument.Parse(json);
            var items = parsed.GetPropertyArrayOrEmpty(arrayName);

            foreach (var item in items)
            {
                var url = item.GetPropertyStringOrNull(urlName);
                if (string.IsNullOrEmpty(url)) continue;
                
                var id = UrlHelpers.IdFromUrl(url, oneKind);
                if (urls != null) urls.Add(url);
                if (ids != null) ids.Add(id);
            }
        }

        #endregion
    }
}
