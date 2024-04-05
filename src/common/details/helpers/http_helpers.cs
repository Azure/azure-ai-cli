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
using System.Threading;
using System.Text.Json;

namespace Azure.AI.Details.Common.CLI
{
    public class HttpHelpers
    {
        public static string DownloadFile(string url)
        {
            return DownloadFile(url, $"Downloading {url}...", null);
        }

        public static string DownloadFile(string url, string message, INamedValues? values)
        {
            var verbose = values != null && values.GetOrDefault("x.verbose", true);
            if (verbose && !string.IsNullOrEmpty(message)) Console.WriteLine(message);

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = WebRequestMethods.Http.Get;

            var key = values?.GetOrDefault("service.config.key", "");
            if (!string.IsNullOrEmpty(key)) request.Headers.Add("Ocp-Apim-Subscription-Key", key);

            var token = values?.GetOrDefault("service.config.token.value", "");
            if (!string.IsNullOrEmpty(token)) request.Headers.Add("Bearer", token);

            var response = (HttpWebResponse)request.GetResponse();
            var stream = response.GetResponseStream();

            int bufferSize = 1024;
            byte[] buffer = new byte[bufferSize];

            var disposition = response.Headers["Content-Disposition"];
            var saveAs = disposition != null && disposition.Contains("filename=") 
                ? disposition.Substring(disposition.IndexOf("filename=") + 9).Trim()
                : null;

            var invalidSaveAs = saveAs == null || File.Exists(saveAs) ||
                saveAs.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0;
            if (invalidSaveAs) saveAs = Path.GetTempFileName();
            var fileStream = FileHelpers.Create(saveAs!);

            int read = 0;
            while ((read = stream.Read(buffer, 0, bufferSize)) != 0) 
            {
                fileStream.Write(buffer, 0, read);
            }

            fileStream.Dispose();
            stream.Dispose();
            response.Dispose();

            if (verbose && !string.IsNullOrEmpty(message)) Console.WriteLine($"{message} Done!\n");

            return saveAs!;
        }

        public static string? DownloadFileWithRetry(string url, int timeOutRetries = 10)
        {
            string? downloaded = null;
            TryCatchHelpers.TryCatchRetry<WebException>(
                () => downloaded = DownloadFile(url),
                (ex) => ex.Message.Contains("timed") && ex.Message.Contains("out"),
                timeOutRetries);
            return downloaded;
        }

        public static string? DownloadFileWithRetry(string url, string message, INamedValues values, int timeOutRetries = 10)
        {
            string? downloaded = null;
            TryCatchHelpers.TryCatchRetry<WebException>(
                () => downloaded = DownloadFile(url, message, values),
                (ex) => ex.Message.Contains("timed") && ex.Message.Contains("out"),
                timeOutRetries);
            return downloaded;
        }

        public static string GetWebRequestNoBodyAsString(HttpWebRequest request)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"{request.Method} {request.RequestUri} HTTP/1.1");
            foreach (var key in request.Headers.AllKeys)
            {
                sb.AppendLine($"{key}: {request.Headers[key]}");
            }
            sb.AppendLine($"Host: {request.Host}");
            sb.AppendLine($"Connection: Keep-Alive");
            sb.AppendLine();
            return sb.ToString();
        }

        public static string? WriteOutputRequest(HttpWebRequest request, string filename, string? payload = null, bool append = false)
        {
            var text = GetWebRequestNoBodyAsString(request);

            if (!append) FileHelpers.WriteAllText(filename, text, Encoding.UTF8);
            if (append) FileHelpers.AppendAllText(filename, text, Encoding.UTF8);

            var payloadOk = !string.IsNullOrEmpty(payload);
            if (payloadOk) FileHelpers.AppendAllText(filename, payload!, Encoding.UTF8);
            return payload;
        }

        public static byte[] WriteOutputRequest(HttpWebRequest request, string filename, byte[] payload) 
        {
            var text = GetWebRequestNoBodyAsString(request);

            FileHelpers.WriteAllText(filename, text, Encoding.UTF8);
            FileHelpers.AppendAllBytes(filename, payload);

            return payload;
        }

        public static HttpWebResponse GetWebResponse(HttpWebRequest request, string? payload = null)
        {
            if (!string.IsNullOrEmpty(payload))
            {
                var bytes = Encoding.UTF8.GetBytes(payload.ToArray());
                var uploadStream = request.GetRequestStream();
                uploadStream.Write(bytes, 0, bytes.Length);
            }

            return (HttpWebResponse)request.GetResponse();
        }

        public static HttpWebResponse GetWebResponse(HttpWebRequest request, byte[] bytes)
        {
            var uploadStream = request.GetRequestStream();
            uploadStream.Write(bytes, 0, bytes.Length);
            return (HttpWebResponse)request.GetResponse();
        }

        public static bool CheckWaitForComplete(HttpWebResponse created, string statusJson, Func<HttpWebRequest> createWebRequest, INamedValues values, string domain, string message, bool? quiet = null, bool? verbose = null)
        {
            // if we're checking a "created" response...
            if (created != null)
            {
                var statusOk = created.StatusCode >= HttpStatusCode.OK && created.StatusCode <= HttpStatusCode.Accepted;
                if (!statusOk) return false;
            }

            // if we're checking a "status" json...
            if (statusJson != null)
            {
                var parsed = JsonDocument.Parse(statusJson);
                var status = parsed.GetPropertyStringOrNull("status");
                var completed = status == "Success" || status == "Succeeded" || status == "Failed" ||
                                status == "success" || status == "succeeded" || status == "failed";
                if (completed) return completed;
            }

            return HttpHelpers.WaitForComplete(createWebRequest, values, domain, message);
        }

        public static bool WaitForComplete(Func<HttpWebRequest> createWebRequest, INamedValues values, string domain, string message, bool? quiet = null, bool? verbose = null)
        {
            quiet ??= values.GetOrDefault("x.quiet", false);
            verbose ??= values.GetOrDefault("x.verbose", true);

            var waitTimeout = values.GetOrDefault($"{domain}.wait.timeout", 0);
            if (waitTimeout <= 0) return false;

            if (!quiet.Value) Console.Write(message);

            var passed = false;
            var keepTrying = true;
            var started = DateTime.Now;
            var finalJson = "";
            while (keepTrying)
            {
                var request = createWebRequest();
                var response = HttpHelpers.GetWebResponse(request);

                var json = HttpHelpers.ReadWriteJson(response, values, domain);
                var parsed = json != null ? JsonDocument.Parse(json) : null;

                var status = parsed?.GetPropertyStringOrNull("status");
                switch (status)
                {
                    case "success":
                    case "Success":
                    case "Succeeded":
                    case "succeeded":
                        message = $"\n{message} Done!\n";
                        keepTrying = false;
                        finalJson = json;
                        passed = true;
                        continue;

                    case "failed":
                    case "Failed":
                        message = $"\n{message} Failed!\n";
                        keepTrying = false;
                        finalJson = json;
                        continue;
                }

                if (DateTime.Now.Subtract(started).TotalMilliseconds > waitTimeout)
                {
                    message = $"\n{message} Timed-out!\n";
                    keepTrying = false;
                    finalJson = json;
                    continue;
                }

                Thread.Sleep(500);
                if (!quiet.Value) Console.Write(".");
                if (!quiet.Value && verbose.Value) Console.Write(status);
            }

            if (!quiet.Value) Console.WriteLine(message);
            if (!quiet.Value && verbose.Value) JsonHelpers.PrintJson(finalJson);

            values.Reset("passed", passed ? "true" : "false");
            return passed;
        }

        public static string? ReadWriteJson(WebResponse? response, INamedValues values, string domain, bool skipWrite = false)
        {
            if (response == null) return null;

            var stream = response.GetResponseStream();
            var isJson = response.ContentType.Contains("application/json");

            var text = FileHelpers.ReadTextWriteIfJson(stream, isJson, values, domain, skipWrite);
            return isJson ? text : null;
        }

        public static string GetOutputDataFileName(string? defaultFileName, HttpWebResponse response, ICommandValues values, string domain, out bool isText, out bool isJson)
        {
            isText = response.ContentType.Contains("text/plain");
            isJson = response.ContentType.Contains("application/json");
            if (string.IsNullOrEmpty(defaultFileName))
            {
                defaultFileName = isJson
                    ? values.GetOrDefault($"{domain}.output.json.file", null)
                    : HttpHelpers.GetFileNameFromResponse(response, values);
            }

            var fileName = values.GetOrDefault($"{domain}.output.file", defaultFileName)!;
            return FileHelpers.GetOutputDataFileName(fileName, values);
        }

        public static string GetFileNameFromResponse(WebResponse response, INamedValues values)
        {
            var runtime = values.GetOrEmpty("x.run.time");
            var defaultFileName = $"{runtime}.downloaded";

            var path = response.ResponseUri.LocalPath;
            var lastSlash = path.LastIndexOfAny("/\\".ToCharArray());
            var lastPart = lastSlash >= 0 ? path[(lastSlash + 1)..] : path;

            var lastPartValid = !string.IsNullOrEmpty(lastPart);
            return lastPartValid ? lastPart : defaultFileName;
        }

        public static string? ReadWriteResponse(WebResponse response, string fileName, string? message, bool returnAsText)
        {
            Stream stream;
            using (stream = response.GetResponseStream())
            return FileHelpers.ReadWriteAllStream(stream, fileName, message, returnAsText);
        }

        public static string? GetLatestVersionInfo(INamedValues values, string domain)
        {
            try
            {
                var uri = "https://api.nuget.org/v3-flatcontainer/azure.ai.cli/index.json";
                var request = (HttpWebRequest)WebRequest.Create(uri);
                request.Method = WebRequestMethods.Http.Get;
                var response = HttpHelpers.GetWebResponse(request);
                var json = HttpHelpers.ReadWriteJson(response, values, domain);
                var info = !string.IsNullOrEmpty(json) ? JsonDocument.Parse(json) : null;
                var versionList = info?.GetPropertyArrayOrEmpty("versions");

                return versionList?.Last().GetString();
            }
            catch (Exception)
            {
                // Report no exception, this is a non-critical operation
            }
            return null;
        }

    }
}
