//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

namespace Azure.AI.Details.Common.CLI
{
    public class BatchCommand : Command
    {
        public BatchCommand(ICommandValues values) : base(values)
        {
            _quiet = _values.GetOrDefault("x.quiet", false);
            _verbose = _values.GetOrDefault("x.verbose", true);
        }

        public bool RunCommand()
        {
            try
            {
                RunBatchCommand();
            }
            catch (WebException ex)
            {
                ConsoleHelpers.WriteLineError($"\n  ERROR: {ex.Message}");
                JsonHelpers.PrintJson(HttpHelpers.ReadWriteJson(ex.Response, _values, "batch"));
            }

            return _values.GetOrDefault("passed", true);
        }

        private bool RunBatchCommand()
        {
            DoCommand(_values.GetCommand());
            return _values.GetOrDefault("passed", true);
        }

        private void DoCommand(string command)
        {
            CheckPath();

            switch (command.Replace("speech.", ""))
            {
                case "batch.list": DoList(); break;
                case "batch.download": DoDownload(); break;

                case "batch.transcription.download": DoDownload(); break;
                case "batch.transcription.create": DoCreateTranscription(); break;
                case "batch.transcription.update": DoUpdateTranscription(); break;
                case "batch.transcription.delete": DoDeleteTranscription(); break;
                case "batch.transcription.status": DoTranscriptionStatus(); break;
                case "batch.transcription.list": DoList("transcriptions"); break;

                case "batch.transcription.onprem.create": DoCreateOnPremTranscription(); break;
                case "batch.transcription.onprem.status": DoOnPremTranscriptionStatus(); break;
                case "batch.transcription.onprem.list": DoOnPremTranscriptionList(); break;
                case "batch.transcription.onprem.delete": DoDeleteOnPremTranscription(); break;
                case "batch.transcription.onprem.endpoints": DoApplyOnPremEndpoints(); break;

                default:
                    _values.AddThrowError("WARNING:", $"'{command.Replace('.', ' ')}' NOT YET IMPLEMENTED!!");
                    break;
            }
        }

        private void DoOnPremTranscriptionList()
        {
            RunOnPremBatchkitIdempotent();

            var request = (HttpWebRequest)WebRequest.Create($"{GetOnPremBatchUrl()}/list");
            request.Method = "GET";
            request.Accept = "application/json";
            CheckWriteOutputRequest(request);
            var response = HttpHelpers.GetWebResponse(request);
            var json = ReadWritePrintJson(response);

            if (!_quiet) Console.WriteLine($"Fetched onprem batch transcriptions.\n");
            
            var batchPaths = new StringBuilder();
            var parsed = JsonDocument.Parse(json);
            var items = parsed.GetPropertyArrayOrEmpty("values");
            foreach (var item in items)
            {
                var batchPath = item.GetPropertyStringOrNull("self");
                batchPaths.AppendLine(batchPath);
            }

            var output = _values.GetOrEmpty("batch.transcription.onprem.outfile");
            if (!string.IsNullOrEmpty(output))
            {
                var batchPathsFile = FileHelpers.GetOutputDataFileName(output, _values);
                FileHelpers.WriteAllText(batchPathsFile, batchPaths.ToString(), Encoding.UTF8);
            }
            if (!_quiet) Console.WriteLine($"{batchPaths.ToString()}\n");
        }

        private void DoDeleteOnPremTranscription()
        {
            RunOnPremBatchkitIdempotent();

            var id = _values.GetOrEmpty("batch.transcription.onprem.id");

            var message = $"Deleting onprem transcription batch '{id}' ...";
            if (!_quiet) Console.WriteLine(message);

            var request = (HttpWebRequest)WebRequest.Create($"{GetOnPremBatchUrl()}/delete?batch_id={id}");
            request.Method = "PUT";
            request.Accept = "application/json";

            var payload = CheckWriteOutputRequest(request);
            var response = HttpHelpers.GetWebResponse(request, payload);

            if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Accepted) {
                if (!_quiet) Console.WriteLine($"{message} Done!\n");
            }
            else {
                _values.AddThrowError("ERROR:", $"Service could not delete batch id {id}. Status code: {response.StatusCode}");
            }
        }

        private void DoOnPremTranscriptionStatus()
        {
            RunOnPremBatchkitIdempotent();

            string id = _values.GetOrEmpty("batch.transcription.onprem.id");
            int waitTimeout = _values.GetOrDefault("batch.transcription.onprem.status.waitms", 0);

            var message = waitTimeout <= 0 ? 
                $"Getting status for transcription batch {id} ..." :
                $"Waiting for transcription batch {id} ...";
            if (!_quiet) Console.WriteLine(message);

            string url = $"{GetOnPremBatchUrl()}" + (waitTimeout <= 0 ?
                    $"/status?batch_id={id}" :
                    $"/watch?batch_id={id}&target_state=done");
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Accept = "application/json";
            if (waitTimeout > 0) { request.Timeout = waitTimeout; }

            CheckWriteOutputRequest(request);
            var response = HttpHelpers.GetWebResponse(request);

            if (response.StatusCode == HttpStatusCode.NotFound) {
                _values.AddThrowError("ERROR:", $"Could not find transcription batch {id}.");
            }
            else if (response.StatusCode != HttpStatusCode.OK) {
                _values.AddThrowError("ERROR:", $"Error retrieving status for transription batch {id}. Status code: {response.StatusCode}");
            }

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            var json = ReadWritePrintJson(response);

            var output = _values.GetOrEmpty("batch.transcription.onprem.outfile");
            if (!string.IsNullOrEmpty(output))
            {
                var statusFile = FileHelpers.GetOutputDataFileName(output, _values);
                FileHelpers.WriteAllText(statusFile, json, Encoding.UTF8);
            }
        }

        private void DoCreateOnPremTranscription()
        {
            RunOnPremBatchkitIdempotent();

            if (!_quiet) Console.WriteLine($"Creating on-prem transcription batch ...");

            var batchSpec = new Dictionary<string, object>();
            batchSpec["type"] = "SpeechSDKBatchRequest";

            var filepaths = _values.GetOrEmpty("batch.transcription.onprem.create.files");
            batchSpec["files"] = filepaths.Split(",;\r\n".ToCharArray()).ToList();

            batchSpec["language"] = _values.GetOrDefault("batch.transcription.onprem.create.language", "en-US");
            batchSpec["diarization"] = _values.GetOrDefault("batch.transcription.onprem.create.diarization", "None");
            batchSpec["nbest"] = Int32.Parse(_values.GetOrDefault("batch.transcription.onprem.create.nbest", "1"));
            batchSpec["profanity"] = _values.GetOrDefault("batch.transcription.onprem.create.profanity", "Masked");
            batchSpec["allow_resume"] = _values.GetOrDefault("batch.transcription.onprem.create.resume", "true");
            if ((string)batchSpec["allow_resume"] == "") { batchSpec["allow_resume"] = "true"; }
            batchSpec["combine_results"] = _values.GetOrDefault("batch.transcription.onprem.create.combine", "false");
            if ((string)batchSpec["combine_results"] == "") { batchSpec["combine_results"] = "true"; }
            batchSpec["sentiment"] = "false";

            var request = (HttpWebRequest)WebRequest.Create($"{GetOnPremBatchUrl()}/submit");
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Accept = "application/json";
            var payload = JsonSerializer.Serialize(batchSpec);
            if (!_quiet) Console.WriteLine("Serialized job submission payload: " + payload);
            payload = CheckWriteOutputRequest(request, payload);

            var response = HttpHelpers.GetWebResponse(request, payload);

            if (response.StatusCode != HttpStatusCode.OK) {
                _values.AddThrowError("ERROR:", $"Error submitting batch for transcription. Status code: {response.StatusCode}");
            }

            var json = ReadWritePrintJson(response);

            var output = _values.GetOrEmpty("batch.transcription.onprem.outfile");
            if (!string.IsNullOrEmpty(output))
            {
                var statusFile = FileHelpers.GetOutputDataFileName(output, _values);
                FileHelpers.WriteAllText(statusFile, json, Encoding.UTF8);
            }
        }

        private void DoApplyOnPremEndpoints() 
        {
            var config = _values.GetOrEmpty("batch.transcription.onprem.endpoints.config");
            if (string.IsNullOrEmpty(config))
            {
                _values.AddThrowError(
                    "WARNING:", "Invalid endpoint config file", "",
                        "USE:", $"{Program.Name} batch transcription onprem endpoints --config /path/to/config.yaml");
            }
            
            File.Copy(config, "/batchkit_config/config.yaml");

            RunOnPremBatchkitIdempotent();
        }

        private void RunOnPremBatchkitIdempotent()
        {
            string pidMarker = "/tmp/.batchkit";
            if (File.Exists(pidMarker)) { 
                Console.WriteLine("Batchkit already running (singleton instance lock held). File lock with PID: " + pidMarker);
                return; 
            }

            // Attempt to create a new file as lock. Could still have race condition detected as IOException.
            FileStream stream = null;
            var enc = new UnicodeEncoding();
            try {
                 stream = new FileStream(pidMarker, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read);
            }
            catch(IOException) {
                if (!_quiet) { Console.WriteLine("Batchkit already running (raced to instantiate)."); }
                return;
            }

            bool success = false;
            Exception exception = null;
            try {
                // Here, on the hook for creating batchkit process.
                Process p = new Process();
                ProcessStartInfo info = new ProcessStartInfo("run-batch-client");
                info.Arguments = "-config /batchkit_config/config.yaml --log-folder /batchkit_logs --console-log-level DEBUG --file-log-level DEBUG --run-mode APISERVER --apiserver_port 5000";
                info.RedirectStandardInput = false;
                info.RedirectStandardOutput = true;
                info.UseShellExecute = false;
                p.StartInfo = info;
                p.Start();
                Thread.Sleep(3000);
                
                var writeStr = p.Id.ToString();
                stream.Write(enc.GetBytes(writeStr), 0, enc.GetByteCount(writeStr));
                stream.Flush();
                success = true;
            }
            catch (Exception e) {
                exception = e;
                if (!_quiet) { Console.WriteLine($"Failure while trying to start and lock batchkit: {e.ToString()}"); }
            }
            finally {
                stream.Close();
                stream.Dispose();
            }

            if (!success) {
                // Ensure we delete the lock file. This could fail because of race condition
                // and we want to raise the original exception instead of this attempt.
                try {
                    File.Delete(pidMarker);
                }
                catch {}
                throw exception;
            }
        }

        private void DoList(string kind = "")
        {
            GetListParameters(kind, out string path, out string message, out string query);

            if (!_quiet) Console.WriteLine(message);

            var request = CreateWebRequest(WebRequestMethods.Http.Get, path, null, query);
            var payload = CheckWriteOutputRequest(request);
            var response = HttpHelpers.GetWebResponse(request, payload);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            var json = ReadWritePrintJson(response, true);
            var parsed = JsonDocument.Parse(json);

            var nextLink = parsed.GetPropertyStringOrNull("@nextLink");
            if (!string.IsNullOrEmpty(nextLink) && !query.Contains("top="))
            {
                var allPages = new List<JsonElement>();
                allPages.Add(parsed.RootElement);

                for (; ;)
                {
                    message = $"Following @nextLink {nextLink} ...";
                    if (!_quiet) Console.WriteLine(message);

                    var requestNext = CreateWebRequest(WebRequestMethods.Http.Get, nextLink, null, null);
                    var payloadNext = CheckWriteOutputRequest(requestNext, null, true);
                    var responseNext = HttpHelpers.GetWebResponse(requestNext, payload);

                    if (!_quiet) Console.WriteLine($"{message} Done!\n");

                    var jsonNext = ReadWritePrintJson(responseNext, true);
                    var parsedNext = JsonDocument.Parse(jsonNext);
                    var thisPage = parsedNext.RootElement;
                    allPages.Add(thisPage);

                    nextLink = thisPage.GetPropertyStringOrNull("@nextLink");
                    if (string.IsNullOrEmpty(nextLink)) break;
                }

                json = JsonHelpers.MergeJsonObjects(allPages);
            }

            FileHelpers.CheckOutputJson(json, _values, "batch");
            UrlHelpers.CheckWriteOutputUrlsOrIds(json, "values", "self", _values, "batch");
        }

        private string DoDownload()
        {
            var url = _values.GetOrEmpty("batch.download.url");
            var urlOk = !string.IsNullOrEmpty(url);
            if (urlOk) return DownloadUrl(url);

            GetDownloadParameters(out string path, out string query, out string message);

            if (!_quiet) Console.WriteLine(message);

            var request = CreateWebRequest(WebRequestMethods.Http.Get, path, null, query);
            var payload = CheckWriteOutputRequest(request);
            var response = HttpHelpers.GetWebResponse(request, payload);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            if (!response.ContentType.Contains("application/json"))
            {
                _values.AddThrowError(
                    "WARNING:", $"Service did not return valid json payload!", "",
                        "TRY:", $"{Program.Name} batch download --url URL");
            }

            var json = ReadWritePrintJson(response);
            var parsed = JsonDocument.Parse(json);

            var fileUrl = parsed.GetPropertyElementOrNull("links")?.GetPropertyStringOrNull("contentUrl") ?? string.Empty;
            var fileName = parsed.GetPropertyStringOrNull("name");
            return DownloadUrl(fileUrl, fileName);
        }

        private void DoCreateTranscription()
        {
            var name = _values.GetOrEmpty("batch.transcription.name");
            if (string.IsNullOrEmpty(name))
            {
                _values.AddThrowError(
                    "WARNING:", "Cannot create transcription; transcription name not found!", "",
                        "USE:", $"{Program.Name} batch transcription create --name NAME [...]");
            }

            var message = $"Creating transcription '{name}' ...";
            if (!_quiet) Console.WriteLine(message);

            var request = CreateWebRequest(WebRequestMethods.Http.Post, "transcriptions");
            var payload = CheckWriteOutputRequest(request, GetCreateTranscriptionPostJson(name));
            var response = HttpHelpers.GetWebResponse(request, payload);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            var json = ReadWritePrintJson(response);
            var url = UrlHelpers.CheckWriteOutputUrlOrId(json, "self", _values, "batch");
            var id = UrlHelpers.IdFromUrl(url);
            CheckWaitForComplete(response, null, "batch", "transcription", id);
        }

        private void DoUpdateTranscription()
        {
            var id = _values.GetOrEmpty("batch.transcription.id");

            var message = $"Updating transcription '{id}' ...";
            if (!_quiet) Console.WriteLine(message);

            var request = CreateWebRequest("PATCH", "transcriptions", id);
            var payload = CheckWriteOutputRequest(request, GetUpdateTranscriptionPostJson());
            var response = HttpHelpers.GetWebResponse(request, payload);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            ReadWritePrintJson(response);
        }

        private void DoDeleteTranscription()
        {
            var id = _values.GetOrEmpty("batch.transcription.id");

            var message = $"Deleting transcription '{id}' ...";
            if (!_quiet) Console.WriteLine(message);

            var request = CreateWebRequest("DELETE", "transcriptions", id);
            var payload = CheckWriteOutputRequest(request);
            var response = HttpHelpers.GetWebResponse(request, payload);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            ReadWritePrintJson(response);
        }

        private void DoTranscriptionStatus()
        {
            var id = _values.GetOrEmpty("batch.transcription.id");

            var message = $"Getting status for transcription {id} ...";
            if (!_quiet) Console.WriteLine(message);

            var request = CreateWebRequest(WebRequestMethods.Http.Get, "transcriptions", id);
            var payload = CheckWriteOutputRequest(request);
            var response = HttpHelpers.GetWebResponse(request, payload);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            var json = ReadWritePrintJson(response);
            UrlHelpers.CheckWriteOutputUrlOrId(json, "self", _values, "batch");
            CheckWaitForComplete(null, json, "batch", "transcription", id);
        }

        private void GetListParameters(string kind, out string path, out string message, out string query)
        {
            kind = _values.GetOrDefault("batch.list.kind", kind);

            var listLanguages = _values.GetOrDefault("batch.list.languages", false);
            var languageKind = _values.GetOrDefault("batch.list.languages.kind", listLanguages ? kind.TrimEnd('s') : "");

            var listId = _values.GetOrEmpty("download.list.id");
            var transcriptionId = IdHelpers.GetIdFromNamedValue(_values, "batch.transcription.id", listId);
            var transcriptionFiles = _values.GetOrDefault("batch.list.transcription.files", false);
            if (transcriptionFiles && string.IsNullOrEmpty(transcriptionId))
            {
                _values.AddThrowError(
                    "WARNING:", $"Cannot list transcription files; transcription id required!", "",
                        "USE:", $"{Program.Name} batch transcription list --transcription files --transcription id ID [...]");
            }

            path = "";
            message = "";
            if (transcriptionFiles)
            {
                path = $"transcriptions/{transcriptionId}/files";
                message = "Listing transcription files ...";
            }
            else if (!string.IsNullOrEmpty(languageKind))
            {
                path = $"{languageKind}s/locales";
                message = $"Listing {languageKind} languages ...";
            }
            else if (!string.IsNullOrEmpty(kind))
            {
                path = kind;
                message = $"Listing {kind} ...";
            }
            else
            {
                _values.AddThrowError(
                    "WARNING:", $"Couldn't find resource type to list!", "",
                        "SEE:", $"{Program.Name} help batch transcription");
            }

            var top = _values.GetOrEmpty("batch.top");
            var skip = _values.GetOrEmpty("batch.skip");

            query = "";
            if (!string.IsNullOrEmpty(skip)) query += $"&skip={skip}";
            if (!string.IsNullOrEmpty(top)) query += $"&top={top}";
            query = query.Trim('&');
        }

        private void GetDownloadParameters(out string path, out string query, out string message)
        {
            path = query = message = "";
            CheckDownloadFile(ref path, ref message);
            CheckDownloadTranscriptionFile(ref path, ref message);

            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(message))
            {
                var command = _values.GetCommandForDisplay();
                _values.AddThrowError(
                    "WARNING:", $"Couldn't determine what to download!", "",
                        "SEE:", $"{Program.Name} help {command}");
            }
        }

        private void CheckDownloadFile(ref string path, ref string message)
        {
            var file = _values.GetOrEmpty("batch.download.file");
            var fileOk = !string.IsNullOrEmpty(file) && file.StartsWith("http");

            if (fileOk)
            {
                path = file;
                message = file.Substring(0, Math.Min(100, file.Length));
                message = file.Length > 100
                    ? $"Locating file {message}... "
                    : $"Locating file {message} ... ";
            }
        }

        private void CheckDownloadTranscriptionFile(ref string path, ref string message)
        {
            var transcriptionFile = _values.GetOrEmpty("batch.transcription.file.id");
            var transcriptionFileOk = !string.IsNullOrEmpty(transcriptionFile) && transcriptionFile.StartsWith("http");

            var downloadId = _values.GetOrEmpty("batch.download.id");
            var transcriptionId = IdHelpers.GetIdFromNamedValue(_values, "batch.transcription.id", downloadId);
            var transcriptionFileId = IdHelpers.GetIdFromNamedValue(_values, "batch.transcription.file.id");

            var transcriptionIdOk = !string.IsNullOrEmpty(transcriptionId);
            var transcriptionFileIdOk = !string.IsNullOrEmpty(transcriptionFileId);

            if (transcriptionFileOk)
            {
                path = transcriptionFile;
                message = $"Locating transcription file {transcriptionFile} ...";
            }
            else if (transcriptionIdOk && transcriptionFileIdOk)
            {
                path = $"transcriptions/{transcriptionId}/files/{transcriptionFileId}";
                message = $"Locating transcription file {transcriptionFileId} ...";
            }
        }

        private string GetCreateTranscriptionPostJson(string name)
        {
            var projectId = _values.GetOrEmpty("batch.project.id");

            var region = _values.GetOrEmpty("service.config.region");
            var projectUrl = GetCustomSpeechUrl(region, "projects", projectId);
            var projectRef = !string.IsNullOrEmpty(projectId) ? $"\"project\": {{ \"self\": \"{projectUrl}\" }}," : "";

            var modelId = _values.GetOrEmpty("batch.transcription.create.model.id");
            var modelUrl = GetCustomSpeechUrl(region, "models", modelId);
            var modelRef = !string.IsNullOrEmpty(modelId) ? $"\"model\": {{ \"self\": \"{modelUrl}\" }}, " : "";

            var datasetId = _values.GetOrEmpty("batch.transcription.create.dataset.id");
            var datasetUrl = GetCustomSpeechUrl(region, "datasets", datasetId);
            var datasetRef = !string.IsNullOrEmpty(datasetId) ? $"\"dataset\": {{ \"self\": \"{datasetUrl}\" }}, " : "";

            StringBuilder sb = new StringBuilder();
            var urls = _values.GetOrDefault("batch.transcription.create.content.urls", _values.GetOrEmpty("batch.transcription.create.content.url"));
            var urlList = urls.Split(";\r\n".ToCharArray()).ToList();
            Predicate<string> fileLikeUrl = url => !url.StartsWith("@") && !url.StartsWith("http");
            
            if (urlList.Exists(fileLikeUrl))
            {
                var url = urlList.Find(fileLikeUrl);
                _values.AddThrowError(
                    "WARNING:", $"Cannot create transcription with content='{url}'",
                                "",
                        "USE:", $"{Program.Name} batch transcription create [...] --content URL",
                                $"{Program.Name} batch transcription create [...] --content URL1;URL1[;...]",
                                $"{Program.Name} batch transcription create [...] --content @URLs.txt");
            }
            foreach (var url in urlList)
            {
                if (!string.IsNullOrEmpty(url))
                {
                    sb.Append($"\"{url}\", ");
                }
            }

            var contentUrls = sb.ToString().Trim(',', ' ');
            var contentUrlRefs = contentUrls.Length > 0 ? $"\"contentUrls\": [ {contentUrls} ]," : "";

            var language = _values.GetOrDefault("batch.transcription.language", "en-US");
            var description = _values.GetOrEmpty("batch.transcription.description");

            return $"{{ {projectRef} {modelRef} {datasetRef} {contentUrlRefs} \"locale\": \"{language}\", \"displayName\": \"{name}\", \"description\": \"{description}\" }}";
        }

        private string GetUpdateTranscriptionPostJson()
        {
            var name = _values.GetOrEmpty("batch.transcription.name");
            var description = _values.GetOrEmpty("batch.transcription.description");

            var region = _values.GetOrEmpty("service.config.region");
            var projectId = _values.GetOrEmpty("batch.project.id");
            var projectUrl = GetCustomSpeechUrl(region, "projects", projectId);
            var projectRef = !string.IsNullOrEmpty(projectId) ? $"\"project\": {{ \"self\": \"{projectUrl}\" }}," : "";

            return $"{{ {projectRef} \"displayName\": \"{name}\", \"description\": \"{description}\" }}";
        }

        private HttpWebRequest CreateWebRequest(string method, string path, string id = null, string query = null, string contentType = null)
        {
            var key = _values.GetOrEmpty("service.config.key");
            var region = _values.GetOrEmpty("service.config.region");
            var timeout = _values.GetOrDefault("batch.wait.timeout", 100000);

            if (string.IsNullOrEmpty(region) || string.IsNullOrEmpty(key))
            {
                _values.AddThrowError("ERROR:", $"Creating request ({path}); requires valid region and key.");
            }

            string url = GetCustomSpeechUrl(region, path, id, query);
            var request = (HttpWebRequest)WebRequest.Create(url);

            request.Method = method;
            request.Accept = "application/json";
            request.Headers.Add("Ocp-Apim-Subscription-Key", key);
            request.Timeout = timeout;

            if (!string.IsNullOrEmpty(contentType))
            {
                request.ContentType = contentType;
            }
            else if (method == WebRequestMethods.Http.Post || method == "PATCH")
            {
                request.ContentType = "application/json";
            }

            return request;
        }

        private string GetOnPremBatchUrl()
        {
            var apiserverHost = _values.GetOrDefault("batch.transcription.onprem.api.host", "127.0.0.1");
            var apiserverPort = _values.GetOrDefault("batch.transcription.onprem.api.port", 5000);
            Console.WriteLine("apiserverHost: " + apiserverHost + "   apiserverPort: " + apiserverPort);
            string url = $"http://{apiserverHost}:{apiserverPort}";
            Console.WriteLine("getonprembatchurl(): " + url);
            return url;
        }

        private string GetCustomSpeechUrl(string region, string path, string id = null, string query = null)
        {
            if (path.StartsWith("http")) return path;

            var idIsUrl = id != null && id.StartsWith("http");
            if (idIsUrl && id.Contains(path)) return id;

            var version = _values.GetOrDefault("batch.api.version", "v3.1");
            var pathPart = string.IsNullOrEmpty(id) ? path : $"{path}/{id}";
            var queryPart = string.IsNullOrEmpty(query) ? "" : $"?{query}";

            var endpoint = $"https://{region}.api.cognitive.microsoft.com/speechtotext/{version}";
            endpoint = _values.GetOrDefault("batch.api.endpoint", endpoint);

            return $"{endpoint}/{pathPart}{queryPart}";
        }

        private string CheckWriteOutputRequest(HttpWebRequest request, string payload = null, bool append = false)
        {
            var output = _values.GetOrEmpty("batch.output.request.file");
            if (!string.IsNullOrEmpty(output))
            {
                var fileName = FileHelpers.GetOutputDataFileName(output, _values);
                payload = HttpHelpers.WriteOutputRequest(request, fileName, payload, append);
            }
            return payload;
        }

        private bool CheckWaitForComplete(HttpWebResponse created, string statusJson, string domain, string thing, string id)
        {
            string message = $"Awaiting {thing} {id} success or failure ...";
            HttpWebRequest createWebRequest() => CreateWebRequest(WebRequestMethods.Http.Get, $"{thing}s", id);
            return HttpHelpers.CheckWaitForComplete(created, statusJson, createWebRequest, _values, domain, message, _quiet, _verbose);
        }

        private string DownloadUrl(string url, string defaultFileName = null)
        {
            var message = url.Substring(0, Math.Min(100, url.Length));
            message = url.Length > 100
                ? $"Downloading {message}... "
                : $"Downloading {message} ... ";

            if (!_quiet) Console.WriteLine(message);

            var request = CreateWebRequest(WebRequestMethods.Http.Get, url);
            var response = HttpHelpers.GetWebResponse(request);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            return ReadWritePrintResponse(response, defaultFileName);
        }

        private string ReadWritePrintResponse(HttpWebResponse response, string defaultFileName = null)
        {
            var saveAs = HttpHelpers.GetOutputDataFileName(defaultFileName, response, _values, "batch", out _, out bool isJson);

            var message = !_quiet ? "Saving as" : null;
            var printJson = !_quiet && _verbose && isJson;
            var downloaded = HttpHelpers.ReadWriteResponse(response, saveAs, message, printJson);

            if (printJson) JsonHelpers.PrintJson(downloaded);

            return downloaded;
        }

        private string ReadWritePrintJson(HttpWebResponse response, bool skipWrite = false)
        {
            var json = HttpHelpers.ReadWriteJson(response, _values, "batch", skipWrite);
            if (!_quiet && _verbose) JsonHelpers.PrintJson(json);
            return json;
        }

        private bool _quiet = false;
        private bool _verbose = false;
    }
}
