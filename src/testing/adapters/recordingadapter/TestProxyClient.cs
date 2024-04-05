//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Diagnostics;
using System.Text.Json.Nodes;
using System.IO;

namespace YamlTestAdapter
{
    public class TestProxyClient
    {
        private static readonly HttpClient _httpClient = new HttpClient(new HttpClientHandler()
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        });

        public enum SanitizeLocation
        {
            Header,
            Body,
            Uri
        }

        public static string BaseUrl => Environment.GetEnvironmentVariable("TEST_PROXY_URL")
            ?? "http://localhost:5004";

        /*
        private static async Task Record()
        {
            var recordingId = await StartRecording();
            await SendRequest(recordingId, "record");
            await Task.Delay(TimeSpan.FromSeconds(2));
            await SendRequest(recordingId, "record");
            await StopRecording(recordingId);
        }

        private static async Task Playback()
        {
            var recordingId = await StartPlayback();
            await SendRequest(recordingId, "playback");
            await SendRequest(recordingId, "playback");
            await StopPlayback(recordingId);
        }
        */

        public static Process InvokeProxy()
        {
            var startInfo = new ProcessStartInfo("test-proxy")
            {
                UseShellExecute = false,
            };

            var process = Process.Start(startInfo);

            return process;
        }

        public static void StopProxy(Process process)
        {
            process.Kill();
        }

        public static async Task<string> StartPlayback(string recordingFile)
        {
            Console.WriteLine($"StartPlayback {recordingFile}");

            var message = new HttpRequestMessage(HttpMethod.Post, BaseUrl + "/playback/start");

            var json = "{\"x-recording-file\":\"" + recordingFile + "\"}";
            var content = new System.Net.Http.StringContent(json, Encoding.UTF8, "application/json");
            message.Content = content;

            var response = await _httpClient.SendAsync(message);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                var contentStream = await response.Content.ReadAsStreamAsync();
                using (StreamReader reader = new StreamReader(contentStream))
                {
                    string responseContent = await reader.ReadToEndAsync();
                    throw new Exception($"Failed to start playback {response.StatusCode} {responseContent}");
                }
            }
            var recordingId = response.Headers.GetValues("x-recording-id").Single();
            Console.WriteLine($"  x-recording-id: {recordingId}");
            Console.WriteLine();

            return recordingId;
        }

        public static async Task StopPlayback(string recordingId)
        {
            Console.WriteLine($"StopPlayback {recordingId}");
            Console.WriteLine();

            var message = new HttpRequestMessage(HttpMethod.Post, BaseUrl + "/playback/stop");
            message.Headers.Add("x-recording-id", recordingId);

            var response = await _httpClient.SendAsync(message);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                var contentStream = await response.Content.ReadAsStreamAsync();
                using (StreamReader reader = new StreamReader(contentStream))
                {
                    string responseContent = await reader.ReadToEndAsync();
                    throw new Exception($"Failed to start playback {response.StatusCode} {responseContent}");
                }
            }
        }

        public static async Task<string> StartRecording(string recordingFile)
        {
            Console.WriteLine($"StartRecording {recordingFile}");

            var message = new HttpRequestMessage(HttpMethod.Post, BaseUrl + "/record/start");

            var json = "{\"x-recording-file\":\"" + recordingFile + "\"}";
            var content = new System.Net.Http.StringContent(json, Encoding.UTF8, "application/json");
            message.Content = content;

            var response = await _httpClient.SendAsync(message);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                var contentStream = await response.Content.ReadAsStreamAsync();
                using (StreamReader reader = new StreamReader(contentStream))
                {
                    string responseContent = await reader.ReadToEndAsync();
                    throw new Exception($"Failed to start playback {response.StatusCode} {responseContent}");
                }
            }

            var recordingId = response.Headers.GetValues("x-recording-id").Single();
            Console.WriteLine($"  x-recording-id: {recordingId}");
            Console.WriteLine();

            return recordingId;
        }

        public static async Task StopRecording(string recordingId)
        {
            Console.WriteLine($"StopRecording {recordingId}");
            Console.WriteLine();

            var message = new HttpRequestMessage(HttpMethod.Post, BaseUrl + "/record/stop");
            message.Headers.Add("x-recording-id", recordingId);
            message.Headers.Add("x-recording-save", bool.TrueString);

            var response = await _httpClient.SendAsync(message);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                var contentStream = await response.Content.ReadAsStreamAsync();
                using (StreamReader reader = new StreamReader(contentStream))
                {
                    string responseContent = await reader.ReadToEndAsync();
                    throw new Exception($"Failed to start playback {response.StatusCode} {responseContent}");
                }
            }
        }

        public static Task AddUriSanitizer(string regexToMatch, string replaceValue) => AddSanitizer("UriRegexSanitizer", new JsonObject() { ["value"] = replaceValue, ["regex"] = regexToMatch });

        public static Task AddHeaderSanitizer(string key, string regex, string value)
        {
            var sanitizer = new JsonObject() { ["value"] = value, ["key"] = key };

            if (regex != null)
            {
                sanitizer["regex"] = regex;
            }

            return AddSanitizer("HeaderRegexSanitizer", sanitizer);
        }


        public static Task AddBodySanitizer(string key, string value) => AddSanitizer("BodyRegexSanitizer", new JsonObject() { ["value"] = value, ["regex"] = key });

        private static async Task AddSanitizer(string headerName, JsonObject json)
        {
            var url = "/admin/addsanitizer";
            var message = new HttpRequestMessage(HttpMethod.Post, BaseUrl + url);
            message.Headers.Add("x-abstraction-identifier", headerName);
            message.Content = new StringContent(json.ToJsonString(), Encoding.UTF8, "application/json");
            var result = await _httpClient.SendAsync(message);
            if (result.StatusCode != HttpStatusCode.OK)
            {
                string responseContent = string.Empty;
                if (result.Content != null)
                {
                    try
                    {
                        responseContent = await result.Content.ReadAsStringAsync();
                    }
                    catch { }
                }

                throw new Exception($"Failed to add sanitizer {result.StatusCode} {responseContent}");
            }
        }

        public static async Task ClearSanitizers()
        {
            var url = "/admin/reset";
            var message = new HttpRequestMessage(HttpMethod.Post, BaseUrl + url);
            var result = await _httpClient.SendAsync(message);

            if (result.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception("Failed to clear sanitizers");
            }
        }
        /*
            public static async Task SendRequest(string recordingId, string mode)
            {
                Console.WriteLine("Request");

                var message = new HttpRequestMessage(HttpMethod.Get, _proxy);
                message.Headers.Add("x-recording-id", recordingId);
                message.Headers.Add("x-recording-mode", mode);
                message.Headers.Add("x-recording-upstream-base-uri", _url);

                var response = await _httpClient.SendAsync(message);
                var body = (await response.Content.ReadAsStringAsync());

                Console.WriteLine("Headers:");
                Console.WriteLine($"  Date: {response.Headers.Date.Value.LocalDateTime}");
                Console.WriteLine($"Body: {(body.Replace("\r", string.Empty).Replace("\n", string.Empty).Substring(0, 80))}");
                Console.WriteLine();
            }
        }
        */
    }

}