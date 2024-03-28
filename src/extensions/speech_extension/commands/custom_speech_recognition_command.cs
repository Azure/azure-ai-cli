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
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;

namespace Azure.AI.Details.Common.CLI
{
    public class CustomSpeechRecognitionCommand : Command
    {
        public CustomSpeechRecognitionCommand(ICommandValues values) : base(values)
        {
            _quiet = _values.GetOrDefault("x.quiet", false);
            _verbose = _values.GetOrDefault("x.verbose", true);
        }

        public bool RunCommand()
        {
            try
            {
                RunCustomSpeechCommand();
            }
            catch (WebException ex)
            {
                ConsoleHelpers.WriteLineError($"\n  ERROR: {ex.Message}");
                JsonHelpers.PrintJson(HttpHelpers.ReadWriteJson(ex.Response, _values, "csr"));
            }

            return _values.GetOrDefault("passed", true);
        }

        private bool RunCustomSpeechCommand()
        {
            DoCommand(_values.GetCommandOrEmpty());
            return _values.GetOrDefault("passed", true);
        }

        private void DoCommand(string command)
        {
            CheckPath();

            switch (command.Replace("speech.", ""))
            {
                case "csr.list": DoList(); break;
                case "csr.download": DoDownload(); break;

                case "csr.project.create": DoCreateProject(); break;
                case "csr.project.update": DoUpdateProject(); break;
                case "csr.project.delete": DoDeleteProject(); break;
                case "csr.project.status": DoProjectStatus(); break;
                case "csr.project.list": DoList("projects"); break;

                case "csr.dataset.download": DoDownload(); break;
                case "csr.dataset.create": DoCreateDataset(); break;
                case "csr.dataset.upload": DoUploadDataset(); break;
                case "csr.dataset.update": DoUpdateDataset(); break;
                case "csr.dataset.delete": DoDeleteDataset(); break;
                case "csr.dataset.status": DoDatasetStatus(); break;
                case "csr.dataset.list": DoList("datasets"); break;

                case "csr.model.download": DoDownload(); break;
                case "csr.model.create": DoCreateModel(); break;
                case "csr.model.update": DoUpdateModel(); break;
                case "csr.model.delete": DoDeleteModel(); break;
                case "csr.model.status": DoModelStatus(); break;
                case "csr.model.list": DoList("models"); break;
                case "csr.model.copy": DoModelCopy(); break;

                case "csr.evaluation.create": DoCreateEvaluation(); break;
                case "csr.evaluation.update": DoUpdateEvaluation(); break;
                case "csr.evaluation.delete": DoDeleteEvaluation(); break;
                case "csr.evaluation.status": DoEvaluationStatus(); break;
                case "csr.evaluation.list": DoList("evaluations"); break;

                case "csr.endpoint.download": DoDownload(); break;
                case "csr.endpoint.create": DoCreateEndpoint(); break;
                case "csr.endpoint.update": DoUpdateEndpoint(); break;
                case "csr.endpoint.delete": DoDeleteEndpoint(); break;
                case "csr.endpoint.status": DoEndpointStatus(); break;
                case "csr.endpoint.list": DoList("endpoints"); break;

                default:
                    _values.AddThrowError("WARNING:", $"'{command.Replace('.', ' ')}' NOT YET IMPLEMENTED!!");
                    break;
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

            FileHelpers.CheckOutputJson(json, _values, "csr");
            UrlHelpers.CheckWriteOutputUrlsOrIds(json, "values", "self", _values, "csr");
        }

        private string? DoDownload()
        {
            var url = _values.GetOrEmpty("csr.download.url");
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
                    "WARNING:", $"Service did not return valid json payload!",
                        "TRY:", $"{Program.Name} csr download --url URL");
            }

            var json = ReadWritePrintJson(response);
            var parsed = JsonDocument.Parse(json);

            var fileUrl = parsed.GetPropertyElementOrNull("links")?.GetPropertyStringOrNull("contentUrl") ?? string.Empty;
            var fileName = parsed.GetPropertyStringOrNull("name");
            return DownloadUrl(fileUrl, fileName);
        }

        private void DoCreateProject()
        {
            var name = _values.GetOrEmpty("csr.project.name");
            if (string.IsNullOrEmpty(name))
            {
                _values.AddThrowError(
                    "WARNING:", $"Cannot create project; project name not found!",
                        "USE:", $"{Program.Name} csr project create --name NAME [...]");
            }

            var message = $"Creating project '{name}' ...";
            if (!_quiet) Console.WriteLine(message);

            var request = CreateWebRequest(WebRequestMethods.Http.Post, "projects");
            var payload = CheckWriteOutputRequest(request, GetCreateProjectPostJson(name));
            var response = HttpHelpers.GetWebResponse(request, payload);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            var json = ReadWritePrintJson(response);
            UrlHelpers.CheckWriteOutputUrlOrId(json, "self", _values, "csr");
        }

        private void DoUpdateProject()
        {
            var id = _values.GetOrEmpty("csr.project.id");

            var message = $"Updating project '{id}' ...";
            if (!_quiet) Console.WriteLine(message);

            var request = CreateWebRequest("PATCH", "projects", id);
            var payload = CheckWriteOutputRequest(request, GetUpdateProjectPostJson());
            var response = HttpHelpers.GetWebResponse(request, payload);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            ReadWritePrintJson(response);
        }

        private void DoDeleteProject()
        {
            var id = _values.GetOrEmpty("csr.project.id");

            var message = $"Deleting project '{id}' ...";
            if (!_quiet) Console.WriteLine(message);

            var request = CreateWebRequest("DELETE", "projects", id);
            var payload = CheckWriteOutputRequest(request);
            var response = HttpHelpers.GetWebResponse(request, payload);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            ReadWritePrintJson(response);
        }

        private void DoProjectStatus()
        {
            var id = _values.GetOrEmpty("csr.project.id");

            var message = $"Getting status for project {id} ...";
            if (!_quiet) Console.WriteLine(message);

            var request = CreateWebRequest(WebRequestMethods.Http.Get, "projects", id);
            var payload = CheckWriteOutputRequest(request);
            var response = HttpHelpers.GetWebResponse(request, payload);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            var json = ReadWritePrintJson(response);
            UrlHelpers.CheckWriteOutputUrlOrId(json, "self", _values, "csr");;
        }

        private void DoCreateDataset()
        {
            var name = _values.GetOrEmpty("csr.dataset.name");
            if (string.IsNullOrEmpty(name))
            {
                _values.AddThrowError(
                    "WARNING:", $"Cannot create dataset; dataset name not found!",
                        "USE:", $"{Program.Name} csr dataset create --name NAME [...]");
            }

            var message = $"Start creating dataset '{name}' ...";
            if (!_quiet) Console.WriteLine(message);

            var request = CreateWebRequest(WebRequestMethods.Http.Post, "datasets");
            var payload = CheckWriteOutputRequest(request, GetCreateDatasetPostJson(name));
            var response = HttpHelpers.GetWebResponse(request, payload);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            var json = ReadWritePrintJson(response);
            var url = UrlHelpers.CheckWriteOutputUrlOrId(json, "self", _values, "csr");;
            var id = UrlHelpers.IdFromUrl(url);
            CheckWaitForComplete(response, null, "csr", "dataset", id);
        }

        private void DoUploadDataset()
        {
            var name = _values.GetOrEmpty("csr.dataset.name");
            if (string.IsNullOrEmpty(name))
            {
                _values.AddThrowError(
                    "WARNING:", $"Cannot upload dataset; dataset name not found!",
                        "USE:", $"{Program.Name} csr dataset upload --name NAME [...]");
            }

            var message = $"Start uploading dataset '{name}' ...";
            if (!_quiet) Console.WriteLine(message);

            var boundary = "----WebKitFormBoundaryAbzpBAzkUAgYKeM8";
            var contentType = $"multipart/form-data; boundary={boundary}";
            var request = CreateWebRequest(WebRequestMethods.Http.Post, "datasets/upload", null, null, contentType);

            var formData = GetUploadDatasetPostFormData(name, boundary);
            CheckWriteOutputRequest(request, Encoding.UTF8.GetString(formData));
            
            var response = HttpHelpers.GetWebResponse(request, formData);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            var json = ReadWritePrintJson(response);
            var url = UrlHelpers.CheckWriteOutputUrlOrId(json, "self", _values, "csr");;
            var id = UrlHelpers.IdFromUrl(url);
            CheckWaitForComplete(response, null, "csr", "dataset", id);
        }

        private void DoUpdateDataset()
        {
            var id = _values.GetOrEmpty("csr.dataset.id");

            var message = $"Updating dataset '{id}' ...";
            if (!_quiet) Console.WriteLine(message);

            var request = CreateWebRequest("PATCH", "datasets", id);
            var payload = CheckWriteOutputRequest(request, GetUpdateDatasetPostJson());
            var response = HttpHelpers.GetWebResponse(request, payload);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            ReadWritePrintJson(response);
        }

        private void DoDeleteDataset()
        {
            var id = _values.GetOrEmpty("csr.dataset.id");

            var message = $"Deleting dataset {id} ...";
            if (!_quiet) Console.WriteLine(message);

            var request = CreateWebRequest("DELETE", "datasets", id);
            var payload = CheckWriteOutputRequest(request);
            var response = HttpHelpers.GetWebResponse(request, payload);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            ReadWritePrintJson(response);
        }

        private void DoDatasetStatus()
        {
            var id = _values.GetOrEmpty("csr.dataset.id");
            var projectId = _values.GetOrEmpty("csr.project.id");

            if (!string.IsNullOrEmpty(projectId))
            {
                _values.AddThrowError(
                    "WARNING:", $"Cannot check dataset status for projectid!",
                        "USE:", $"{Program.Name} csr dataset status --id ID [...]");
            }

            var message = $"Getting status for dataset {id}...";
            if (!_quiet) Console.WriteLine(message);

            var request = CreateWebRequest(WebRequestMethods.Http.Get, "datasets", id);
            var payload = CheckWriteOutputRequest(request);
            var response = HttpHelpers.GetWebResponse(request, payload);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            var json = ReadWritePrintJson(response);
            UrlHelpers.CheckWriteOutputUrlOrId(json, "self", _values, "csr");;
            CheckWaitForComplete(null, json, "csr", "dataset", id);
        }

        private void DoCreateModel()
        {
            var name = _values.GetOrEmpty("csr.model.name");
            if (string.IsNullOrEmpty(name))
            {
                _values.AddThrowError(
                    "WARNING:", $"Cannot create model; model name not found!",
                        "USE:", $"{Program.Name} csr model create --name NAME [...]");
            }

            var message = $"Start creating model '{name}' ...";
            if (!_quiet) Console.WriteLine(message);

            var request = CreateWebRequest(WebRequestMethods.Http.Post, "models");
            var payload = CheckWriteOutputRequest(request, GetCreateModelPostJson(name));
            var response = HttpHelpers.GetWebResponse(request, payload);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            var json = ReadWritePrintJson(response);
            var url = UrlHelpers.CheckWriteOutputUrlOrId(json, "self", _values, "csr");;
            var id = UrlHelpers.IdFromUrl(url);
            CheckWaitForComplete(response, null, "csr", "model", id);
        }

        private void DoUpdateModel()
        {
            var id = _values.GetOrEmpty("csr.model.id");

            var message = $"Updating model '{id}' ...";
            if (!_quiet) Console.WriteLine(message);

            var request = CreateWebRequest("PATCH", "models", id);
            var payload = CheckWriteOutputRequest(request, GetUpdateModelPostJson());
            var response = HttpHelpers.GetWebResponse(request, payload);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            ReadWritePrintJson(response);
        }

        private void DoDeleteModel()
        {
            var id = _values.GetOrEmpty("csr.model.id");

            var message = $"Deleting model {id} ...";
            if (!_quiet) Console.WriteLine(message);

            var request = CreateWebRequest("DELETE", "models", id);
            var payload = CheckWriteOutputRequest(request);
            var response = HttpHelpers.GetWebResponse(request, payload);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            ReadWritePrintJson(response);
        }

        private void DoModelStatus()
        {
            var id = _values.GetOrEmpty("csr.model.id");
            var projectId = _values.GetOrEmpty("csr.project.id");

            if (!string.IsNullOrEmpty(projectId))
            {
                _values.AddThrowError(
                    "WARNING:", $"Cannot check model status for projectid!",
                        "USE:", $"{Program.Name} csr model status --id ID [...]");
            }

            var message = $"Getting status for model {id}...";
            if (!_quiet) Console.WriteLine(message);

            var request = CreateWebRequest(WebRequestMethods.Http.Get, "models", id);
            var payload = CheckWriteOutputRequest(request);
            var response = HttpHelpers.GetWebResponse(request, payload);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            var json = ReadWritePrintJson(response);
            UrlHelpers.CheckWriteOutputUrlOrId(json, "self", _values, "csr");;
            CheckWaitForComplete(null, json, "csr", "model", id);
        }

        private void DoModelCopy()
        {
            var id = _values.GetOrEmpty("csr.model.id");
            var projectId = _values.GetOrEmpty("csr.project.id");
            var targetKey = _values.GetOrEmpty("csr.model.copy.target.key");

            if (!string.IsNullOrEmpty(projectId))
            {
                _values.AddThrowError(
                    "WARNING:", $"Cannot copy model for projectid!",
                        "USE:", $"{Program.Name} csr model copy --id ID [...]");
            }
            else if (string.IsNullOrEmpty(targetKey))
            {
                _values.AddThrowError(
                    "WARNING:", $"Cannot copy model; requires valid target subscription key.",
                        "USE:", $"{Program.Name} csr model copy --target key KEY [...]");
            }

            var message = $"Copying model {id}...";
            if (!_quiet) Console.WriteLine(message);

            var request = CreateWebRequest(WebRequestMethods.Http.Post, $"models/{id}/copyto");
            var payload = CheckWriteOutputRequest(request, GetCopyModelPostJson(targetKey));
            var response = HttpHelpers.GetWebResponse(request, payload);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            var json = ReadWritePrintJson(response);
            UrlHelpers.CheckWriteOutputUrlOrId(json, "self", _values, "csr");;
        }

        private void DoCreateEvaluation()
        {
            var name = _values.GetOrEmpty("csr.evaluation.name");
            if (string.IsNullOrEmpty(name))
            {
                _values.AddThrowError(
                    "WARNING:", $"Cannot create evaluation; evaluation name not found!",
                        "USE:", $"{Program.Name} csr evaluation create --name NAME [...]");
            }

            var message = $"Start creating evaluation '{name}' ...";
            if (!_quiet) Console.WriteLine(message);

            var request = CreateWebRequest(WebRequestMethods.Http.Post, "evaluations");
            var payload = CheckWriteOutputRequest(request, GetCreateEvaluationPostJson(name));
            var response = HttpHelpers.GetWebResponse(request, payload);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            var json = ReadWritePrintJson(response);
            var url = UrlHelpers.CheckWriteOutputUrlOrId(json, "self", _values, "csr");;
            var id = UrlHelpers.IdFromUrl(url);
            CheckWaitForComplete(response, null, "csr", "evaluation", id);
        }

        private void DoUpdateEvaluation()
        {
            var id = _values.GetOrEmpty("csr.evaluation.id");

            var message = $"Updating evaluation '{id}' ...";
            if (!_quiet) Console.WriteLine(message);

            var request = CreateWebRequest("PATCH", "evaluations", id);
            var payload = CheckWriteOutputRequest(request, GetUpdateEvaluationPostJson());
            var response = HttpHelpers.GetWebResponse(request, payload);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            ReadWritePrintJson(response);
        }

        private void DoDeleteEvaluation()
        {
            var id = _values.GetOrEmpty("csr.evaluation.id");

            var message = $"Deleting evaluation {id} ...";
            if (!_quiet) Console.WriteLine(message);

            var request = CreateWebRequest("DELETE", "evaluations", id);
            var payload = CheckWriteOutputRequest(request);
            var response = HttpHelpers.GetWebResponse(request, payload);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            ReadWritePrintJson(response);
        }

        private void DoEvaluationStatus()
        {
            var id = _values.GetOrEmpty("csr.evaluation.id");
            var projectId = _values.GetOrEmpty("csr.project.id");

            if (!string.IsNullOrEmpty(projectId))
            {
                _values.AddThrowError(
                    "WARNING:", $"Cannot check evaluation status for projectid!",
                        "USE:", $"{Program.Name} csr evaluation status --id ID [...]");
            }

            var message = $"Getting status for evaluation {id}...";
            if (!_quiet) Console.WriteLine(message);

            var request = CreateWebRequest(WebRequestMethods.Http.Get, "evaluations", id);
            var payload = CheckWriteOutputRequest(request);
            var response = HttpHelpers.GetWebResponse(request, payload);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            var json = ReadWritePrintJson(response);
            UrlHelpers.CheckWriteOutputUrlOrId(json, "self", _values, "csr");;
            CheckWaitForComplete(null, json, "csr", "evaluation", id);
        }

        private void DoCreateEndpoint()
        {
            var name = _values.GetOrEmpty("csr.endpoint.name");
            if (string.IsNullOrEmpty(name))
            {
                _values.AddThrowError(
                    "WARNING:", $"Cannot create endpoint; endpoint name not found!",
                        "USE:", $"{Program.Name} csr endpoint create --name NAME [...]");
            }

            var message = $"Start creating endpoint '{name}' ...";
            if (!_quiet) Console.WriteLine(message);

            var request = CreateWebRequest(WebRequestMethods.Http.Post, "endpoints");
            var payload = CheckWriteOutputRequest(request, GetCreateEndpointPostJson(name));
            var response = HttpHelpers.GetWebResponse(request, payload);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            var json = ReadWritePrintJson(response);
            var url = UrlHelpers.CheckWriteOutputUrlOrId(json, "self", _values, "csr");;
            var id = UrlHelpers.IdFromUrl(url);
            CheckWaitForComplete(response, null, "csr", "endpoint", id);
        }

        private void DoUpdateEndpoint()
        {
            var id = _values.GetOrEmpty("csr.endpoint.id");

            var message = $"Updating endpoint '{id}' ...";
            if (!_quiet) Console.WriteLine(message);

            var request = CreateWebRequest("PATCH", "endpoints", id);
            var payload = CheckWriteOutputRequest(request, GetUpdateEndpointPostJson());
            var response = HttpHelpers.GetWebResponse(request, payload);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            ReadWritePrintJson(response);
        }

        private void DoDeleteEndpoint()
        {
            var id = _values.GetOrEmpty("csr.endpoint.id");

            var message = $"Deleting endpoint {id} ...";
            if (!_quiet) Console.WriteLine(message);

            var request = CreateWebRequest("DELETE", "endpoints", id);
            var payload = CheckWriteOutputRequest(request);
            var response = HttpHelpers.GetWebResponse(request, payload);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            ReadWritePrintJson(response);
        }

        private void DoEndpointStatus()
        {
            var id = _values.GetOrEmpty("csr.endpoint.id");
            var projectId = _values.GetOrEmpty("csr.project.id");

            if (!string.IsNullOrEmpty(projectId))
            {
                _values.AddThrowError(
                    "WARNING:", $"Cannot check endpoint status for projectid!",
                        "USE:", $"{Program.Name} csr endpoint status --id ID [...]");
            }

            var message = $"Getting status for endpoint {id}...";
            if (!_quiet) Console.WriteLine(message);

            var request = CreateWebRequest(WebRequestMethods.Http.Get, "endpoints", id);
            var payload = CheckWriteOutputRequest(request);
            var response = HttpHelpers.GetWebResponse(request, payload);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            var json = ReadWritePrintJson(response);
            UrlHelpers.CheckWriteOutputUrlOrId(json, "self", _values, "csr");;
            CheckWaitForComplete(null, json, "csr", "endpoint", id);
        }

        private void GetListParameters(string kind, out string path, out string message, out string query)
        {
            kind = _values.GetOrDefault("csr.list.kind", kind);
            
            var listLanguages = _values.GetOrDefault("csr.list.languages", false);
            var languageKind = _values.GetOrDefault("csr.list.languages.kind", listLanguages ? kind.TrimEnd('s') : "");

            var list = _values.GetOrEmpty("csr.list.id");
            var listOk = list.StartsWith("http");

            var listId = listOk ? "" : list;

            var dataset = _values.GetOrEmpty("csr.dataset.id");
            var datasetOk = !string.IsNullOrEmpty(dataset) && dataset.StartsWith("http");

            var kindIsDatasets = kind == "datasets";
            var datasetId = IdHelpers.GetIdFromNamedValue(_values, "csr.dataset.id", kindIsDatasets ? listId : "");
            var datasetIdOk = !string.IsNullOrEmpty(datasetId) && !datasetId.StartsWith("http");

            var datasetFiles = _values.GetOrDefault("csr.list.dataset.files", datasetOk || datasetIdOk);
            if (datasetFiles && !datasetOk && !datasetIdOk)
            {
                _values.AddThrowError(
                    "WARNING:", $"Cannot list dataset files without a valid dataset ID/URL!",
                        "USE:", $"{Program.Name} csr dataset list --dataset ID/URL --files");
            }

            var evaluation = _values.GetOrEmpty("csr.evaluation.id");
            var evaluationOk = !string.IsNullOrEmpty(evaluation) && evaluation.StartsWith("http");

            var kindIsEvaluations = kind == "evaluations";
            var evaluationId = IdHelpers.GetIdFromNamedValue(_values, "csr.evaluation.id", kindIsEvaluations ? listId : "");
            var evaluationIdOk = !string.IsNullOrEmpty(evaluationId) && !evaluationId.StartsWith("http");

            var evaluationFiles = _values.GetOrDefault("csr.list.evaluation.files", evaluationOk || evaluationIdOk);
            if (evaluationFiles && !evaluationIdOk && !evaluationIdOk)
            {
                _values.AddThrowError(
                    "WARNING:", $"Cannot list evaluation files without a valid evaluation ID/URL!",
                        "USE:", $"{Program.Name} csr evaluation list --evaluation ID/URL --files");
            }

            var endpoint = _values.GetOrEmpty("csr.endpoint.id");
            var endpointOk = !string.IsNullOrEmpty(endpoint) && endpoint.StartsWith("http");

            var kindIsEndpoints = kind == "endpoints";
            var endpointId = IdHelpers.GetIdFromNamedValue(_values, "csr.endpoint.id", kindIsEndpoints ? listId : "");
            var endpointIdOk = !string.IsNullOrEmpty(endpointId) && !endpointId.StartsWith("http");

            var endpointLogs = _values.GetOrDefault("csr.list.endpoint.logs", endpointOk || endpointIdOk);
            if (endpointLogs && !endpointOk && !endpointIdOk)
            {
                _values.AddThrowError(
                    "WARNING:", $"Cannot list endpoint logs without a valid endpoint ID/URL!",
                        "USE:", $"{Program.Name} csr endpoint list --endpoint ID/URL --logs");
            }

            path = "";
            message = "";
            if (datasetOk)
            {
                path = $"{dataset}/files";
                message = "Listing dataset files ...";
            }
            else if (datasetIdOk)
            {
                path = $"datasets/{datasetId}/files";
                message = "Listing dataset files ...";
            }
            else if (evaluationOk)
            {
                path = evaluation.EndsWith("/files") ? evaluation : $"{evaluation}/files";
                message = "Listing evaluation files ...";
            }
            else if (evaluationIdOk)
            {
                path = $"evaluations/{evaluationId}/files";
                message = "Listing evaluation files ...";
            }
            else if (endpointOk)
            {
                path = endpoint.EndsWith("/files/logs") ? endpoint : $"{endpoint}/files/logs";
                message = "Listing endpoint logs";
            }
            else if (endpointIdOk)
            {
                path = $"endpoints/{endpointId}/files/logs";
                message = "Listing endpoint logs ...";
            }
            else if (listOk && list.Contains("/datasets/"))
            {
                path = list.EndsWith("/files") ? list : $"{list}/files";
                message = "Listing dataset files ...";
            }
            else if (listOk && list.Contains("/evaluations/"))
            {
                path = list.EndsWith("/files") ? list : $"{list}/files";
                message = "Listing evaluation files ...";
            }
            else if (listOk && list.Contains("/endpoints/"))
            {
                path = list.EndsWith("/files") ? list : $"{list}/files";
                message = "Listing endpoint files ...";
            }
            else if (!string.IsNullOrEmpty(languageKind))
            {
                path = $"{languageKind}s/locales";
                message = $"Listing {languageKind} languages ...";
            }
            else if (!string.IsNullOrEmpty(kind))
            {
                var projectId = IdHelpers.GetIdFromNamedValue(_values, "csr.project.id");
                var projectOk = !string.IsNullOrEmpty(projectId);

                path = projectOk ? $"projects/{projectId}/{kind}" : kind;
                message = $"Listing {kind} ...";
            }
            else
            {
                _values.AddThrowError(
                    "WARNING:", $"Couldn't find resource type to list!",
                        "SEE:", $"{Program.Name} help csr");
            }

            var top = _values.GetOrEmpty("csr.top");
            var skip = _values.GetOrEmpty("csr.skip");

            query = "";
            if (!string.IsNullOrEmpty(skip)) query += $"&skip={skip}";
            if (!string.IsNullOrEmpty(top)) query += $"&top={top}";
            query = query.Trim('&');
        }

        private void GetDownloadParameters(out string path, out string query, out string message)
        {
            path = query = message = "";
            CheckDownloadFile(ref path, ref message);
            CheckDownloadDatasetFile(ref path, ref message);
            CheckDownloadEndpointLog(ref path, ref message);
            CheckDownloadEvaluationFile(ref path, ref message);

            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(message))
            {
                var command = _values.GetCommandForDisplay();
                _values.AddThrowError(
                    "WARNING:", $"Couldn't determine what to download!",
                        "SEE:", $"{Program.Name} help {command}");
            }
        }

        private void CheckDownloadFile(ref string path, ref string message)
        {
            var file = _values.GetOrEmpty("csr.download.file");
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

        private void CheckDownloadDatasetFile(ref string path, ref string message)
        {
            var datasetFile = _values.GetOrEmpty("csr.dataset.file.id");
            var datasetFileOk = !string.IsNullOrEmpty(datasetFile) && datasetFile.StartsWith("http");

            var downloadId = _values.GetOrEmpty("csr.download.id");
            var datasetId = IdHelpers.GetIdFromNamedValue(_values, "csr.dataset.id", downloadId);
            var datasetIdOk = !string.IsNullOrEmpty(datasetId);

            var datasetFileId = IdHelpers.IdFromString(datasetFile);
            var datasetFileIdOk = !!string.IsNullOrEmpty(datasetFileId);

            if (datasetFileOk)
            {
                path = datasetFile;
                message = $"Locating dataset file {datasetFile} ...";
            }
            else if (datasetIdOk && datasetFileIdOk)
            {
                path = $"datasets/{datasetId}/files/{datasetFileId}";
                message = $"Locating dataset file {datasetFileId} ...";
            }
        }

        private void CheckDownloadEvaluationFile(ref string path, ref string message)
        {
            var evaluationFile = _values.GetOrEmpty("csr.evaluation.file.id");
            var evaluationFileOk = !string.IsNullOrEmpty(evaluationFile) && evaluationFile.StartsWith("http");

            var downloadId = _values.GetOrEmpty("csr.download.id");
            var evaluationId = IdHelpers.GetIdFromNamedValue(_values, "csr.evaluation.id", downloadId);
            var evaluationIdOk = !string.IsNullOrEmpty(evaluationId);

            var evaluationFileId = IdHelpers.IdFromString(evaluationFile);
            var evaluationFileIdOk = !!string.IsNullOrEmpty(evaluationFileId);

            if (evaluationFileOk)
            {
                path = evaluationFile;
                message = $"Locating evaluation file {evaluationFile} ...";
            }
            else if (evaluationIdOk && evaluationFileIdOk)
            {
                path = $"evaluation/{evaluationId}/files/{evaluationFileId}";
                message = $"Locating evaluation file {evaluationFileId} ...";
            }
        }

        private void CheckDownloadEndpointLog(ref string path, ref string message)
        {
            var endpointLog = _values.GetOrEmpty("csr.endpoint.log.id");
            var endpointLogOk = !string.IsNullOrEmpty(endpointLog) && endpointLog.StartsWith("http");

            var downloadId = _values.GetOrEmpty("csr.download.id");
            var endpointId = IdHelpers.GetIdFromNamedValue(_values, "csr.endpoint.id", downloadId);
            var endpointIdOk = !string.IsNullOrEmpty(endpointId);

            var endpointLogId = IdHelpers.GetIdFromNamedValue(_values, "csr.endpoint.log.id");
            var endpointLogIdOk = !string.IsNullOrEmpty(endpointLogId);

            if (endpointLogOk)
            {
                path = endpointLog;
                message = $"Locating endpoint log {endpointLog} ...";
            }
            else if (endpointIdOk && endpointLogIdOk)
            {
                path = $"endpoints/{endpointId}/files/logs/{endpointLogId}";
                message = $"Locating endpoint log {endpointLogId} ...";
            }
        }

        private string GetCreateProjectPostJson(string name)
        {
            var language = _values.GetOrDefault("csr.project.language", "en-US");
            var description = _values.GetOrEmpty("csr.project.description");
            return $"{{ \"locale\": \"{language}\", \"displayName\": \"{name}\", \"description\": \"{description}\" }}";
        }

        private string GetUpdateProjectPostJson()
        {
            var name = _values.GetOrEmpty("csr.project.name");
            var description = _values.GetOrEmpty("csr.project.description");

            var region = _values.GetOrEmpty("service.config.region");
            var projectId = _values.GetOrEmpty("csr.project.id");
            var projectUrl = GetCustomSpeechUrl(region, "projects", projectId);
            var projectRef = !string.IsNullOrEmpty(projectId) ? $"\"project\": {{ \"self\": \"{projectUrl}\" }}," : "";

            return $"{{ {projectRef} \"displayName\": \"{name}\", \"description\": \"{description}\" }}";
        }

        private string GetCreateModelPostJson(string name)
        {
            var projectId = _values.GetOrEmpty("csr.project.id");

            var region = _values.GetOrEmpty("service.config.region");
            var projectUrl = GetCustomSpeechUrl(region, "projects", projectId);
            var projectRef = !string.IsNullOrEmpty(projectId) ? $"\"project\": {{ \"self\": \"{projectUrl}\" }}," : "";

            var baseId = _values.GetOrEmpty("csr.model.create.base.model.id");
            var baseUrl = GetCustomSpeechUrl(region, "models/base", baseId);
            var baseRef = !string.IsNullOrEmpty(baseId) ? $"\"baseModel\": {{ \"self\": \"{baseUrl}\" }}," : "";

            StringBuilder sb = new StringBuilder();
            var datasetIds = _values.GetOrDefault("csr.model.create.dataset.ids", _values.GetOrEmpty("csr.model.create.dataset.id"));
            foreach (var datasetId in datasetIds.Split(";\r\n".ToCharArray()))
            {
                if (!string.IsNullOrEmpty(datasetId))
                {
                    var datasetUrl = GetCustomSpeechUrl(region, "datasets", datasetId);
                    sb.Append($"{{ \"self\": \"{datasetUrl}\" }}, ");
                }
            }

            var datasets = sb.ToString().Trim(',', ' ');
            var datasetRefs = datasets.Length > 0 ? $"\"datasets\": [ {datasets} ]," : "";

            var text = _values.GetOrEmpty("csr.model.create.text");
            var textRef = !string.IsNullOrEmpty(text) ? $"\"text\": \"{text}\"," : "";

            var language = _values.GetOrDefault("csr.model.language", "en-US");
            var description = _values.GetOrEmpty("csr.model.description");

            return $"{{ {projectRef} {baseRef} {datasetRefs} {textRef} \"locale\": \"{language}\", \"displayName\": \"{name}\", \"description\": \"{description}\" }}";
        }

        private string GetUpdateModelPostJson()
        {
            var name = _values.GetOrEmpty("csr.model.name");
            var description = _values.GetOrEmpty("csr.model.description");

            var region = _values.GetOrEmpty("service.config.region");
            var projectId = _values.GetOrEmpty("csr.project.id");
            var projectUrl = GetCustomSpeechUrl(region, "projects", projectId);
            var projectRef = !string.IsNullOrEmpty(projectId) ? $"\"project\": {{ \"self\": \"{projectUrl}\" }}," : "";

            return $"{{ {projectRef} \"displayName\": \"{name}\", \"description\": \"{description}\" }}";
        }

        private string GetCopyModelPostJson(string targetKey)
        {
            return $"{{ \"targetSubscriptionKey\": \"{targetKey}\" }}\n";
        }

        private string GetCreateEvaluationPostJson(string name)
        {
            var projectId = _values.GetOrEmpty("csr.project.id");

            var region = _values.GetOrEmpty("service.config.region");
            var projectUrl = GetCustomSpeechUrl(region, "projects", projectId);
            var projectRef = !string.IsNullOrEmpty(projectId) ? $"\"project\": {{ \"self\": \"{projectUrl}\" }}," : "";

            var modelId1 = _values.GetOrEmpty("csr.evaluation.create.model1.id");
            var modelUrl1 = GetCustomSpeechUrl(region, "models", modelId1);
            var modelRef1 = !string.IsNullOrEmpty(modelId1) ? $"\"model1\": {{ \"self\": \"{modelUrl1}\" }}," : "";

            var modelId2 = _values.GetOrEmpty("csr.evaluation.create.model2.id");
            var modelUrl2 = GetCustomSpeechUrl(region, "models", modelId2);
            var modelRef2 = !string.IsNullOrEmpty(modelId2) ? $"\"model2\": {{ \"self\": \"{modelUrl2}\" }}," : "";

            var datasetId = _values.GetOrEmpty("csr.evaluation.create.dataset.id");
            var datasetUrl = GetCustomSpeechUrl(region, "datasets", datasetId);
            var datasetRef = !string.IsNullOrEmpty(modelId1) ? $"\"dataset\": {{ \"self\": \"{datasetUrl}\" }}," : "";

            var language = _values.GetOrDefault("csr.evaluation.language", "en-US");
            var description = _values.GetOrEmpty("csr.evaluation.description");

            return $"{{ {projectRef} {modelRef1} {modelRef2} {datasetRef} \"locale\": \"{language}\", \"displayName\": \"{name}\", \"description\": \"{description}\" }}";
        }

        private string GetUpdateEvaluationPostJson()
        {
            var name = _values.GetOrEmpty("csr.evaluation.name");
            var description = _values.GetOrEmpty("csr.evaluation.description");

            var region = _values.GetOrEmpty("service.config.region");
            var projectId = _values.GetOrEmpty("csr.project.id");
            var projectUrl = GetCustomSpeechUrl(region, "projects", projectId);
            var projectRef = !string.IsNullOrEmpty(projectId) ? $"\"project\": {{ \"self\": \"{projectUrl}\" }}," : "";

            return $"{{ {projectRef} \"displayName\": \"{name}\", \"description\": \"{description}\" }}";
        }

        private string GetCreateDatasetPostJson(string name)
        {
            var contentUrl = _values.GetOrEmpty("csr.dataset.create.content.url");
            var projectId = _values.GetOrEmpty("csr.project.id");

            var region = _values.GetOrEmpty("service.config.region");
            var projectUrl = GetCustomSpeechUrl(region, "projects", projectId);
            var projectRef = !string.IsNullOrEmpty(projectId) ? $"\"project\": {{ \"self\": \"{projectUrl}\" }}," : "";

            var kind = _values.GetOrEmpty("csr.dataset.kind");
            var language = _values.GetOrDefault("csr.dataset.language", "en-US");
            var description = _values.GetOrEmpty("csr.dataset.description");

            return $"{{ {projectRef} \"kind\": \"{kind}\", \"contentUrl\": \"{contentUrl}\", \"locale\": \"{language}\", \"displayName\": \"{name}\", \"description\": \"{description}\" }}";
        }

        private byte[] GetUploadDatasetPostFormData(string name, string boundary)
        {
            var dataFile = _values.GetOrEmpty("csr.dataset.upload.data.file");
            if (string.IsNullOrEmpty(dataFile))
            {
                _values.AddThrowError(
                    "WARNING:", $"Cannot upload data; missing data file!",
                        "USE:", $"{Program.Name} csr dataset upload --data FILENAME [...]");
            }

            var zipFile = FileHelpers.FindFileInDataPath(dataFile, _values);
            if (string.IsNullOrEmpty(zipFile))
            {
                _values.AddThrowError("ERROR:", $"Cannot find data file: \"{dataFile}\"");
            }

            var projectId = _values.GetOrEmpty("csr.project.id");

            var region = _values.GetOrEmpty("service.config.region");
            var projectUrl = GetCustomSpeechUrl(region, "projects", projectId);

            var kind = _values.GetOrEmpty("csr.dataset.kind");
            var language = _values.GetOrDefault("csr.dataset.language", "en-US");
            var description = _values.GetOrEmpty("csr.dataset.description");

            var sb = new StringBuilder();

            if (!string.IsNullOrEmpty(projectId)) sb.Append($"--{boundary}\r\nContent-Disposition: form-data; name=\"project\"\r\n\r\n{projectUrl}\r\n");
            sb.Append($"--{boundary}\r\nContent-Disposition: form-data; name=\"kind\"\r\n\r\n{kind}\r\n");
            sb.Append($"--{boundary}\r\nContent-Disposition: form-data; name=\"locale\"\r\n\r\n{language}\r\n");
            sb.Append($"--{boundary}\r\nContent-Disposition: form-data; name=\"displayName\"\r\n\r\n{name}\r\n");
            sb.Append($"--{boundary}\r\nContent-Disposition: form-data; name=\"description\"\r\n\r\n{description}\r\n");

            var contentType = "text/plain";
            switch (kind.ToLower())
            {
                case "audio": contentType = "application/x-zip-compressed"; break;
                case "acoustic": contentType = "application/zip"; break;
            }
            sb.Append($"--{boundary}\r\nContent-Disposition: form-data; name=\"data\"; filename=\"{dataFile}\"\r\nContent-Type: {contentType}\r\n\r\n");

            var bytes1 = Encoding.UTF8.GetBytes(sb.ToString());
            var bytes2 = FileHelpers.ReadAllBytes(zipFile!);
            var bytes3 = Encoding.UTF8.GetBytes($"\r\n--{boundary}--\r\n");

            var bytes = new byte[bytes1.Length + bytes2.Length + bytes3.Length];
            bytes1.CopyTo(bytes, 0);
            bytes2.CopyTo(bytes, bytes1.Length);
            bytes3.CopyTo(bytes, bytes1.Length + bytes2.Length);

            return bytes;
        }

        private string GetUpdateDatasetPostJson()
        {
            var name = _values.GetOrEmpty("csr.dataset.name");
            var description = _values.GetOrEmpty("csr.dataset.description");

            var region = _values.GetOrEmpty("service.config.region");
            var projectId = _values.GetOrEmpty("csr.project.id");
            var projectUrl = GetCustomSpeechUrl(region, "projects", projectId);
            var projectRef = !string.IsNullOrEmpty(projectId) ? $"\"project\": {{ \"self\": \"{projectUrl}\" }}," : "";

            return $"{{ {projectRef} \"displayName\": \"{name}\", \"description\": \"{description}\" }}";
        }

        private string GetCreateEndpointPostJson(string name)
        {
            var projectId = _values.GetOrEmpty("csr.project.id");

            var region = _values.GetOrEmpty("service.config.region");
            var projectUrl = GetCustomSpeechUrl(region, "projects", projectId);
            var projectRef = !string.IsNullOrEmpty(projectId) ? $"\"project\": {{ \"self\": \"{projectUrl}\" }}," : "";

            var modelId = _values.GetOrEmpty("csr.endpoint.create.model.id");
            var modelUrl = GetCustomSpeechUrl(region, "models", modelId);
            var modelRef = !string.IsNullOrEmpty(modelId) ? $"\"model\": {{ \"self\": \"{modelUrl}\" }}, " : "";

            var text = _values.GetOrEmpty("csr.endpoint.create.text");
            var textRef = !string.IsNullOrEmpty(text) ? $"\"text\": \"{text}\"," : "";

            var language = _values.GetOrDefault("csr.endpoint.language", "en-US");
            var description = _values.GetOrEmpty("csr.endpoint.description");
            var properties = GetProperties("endpoint");

            return $"{{ {projectRef} {modelRef} {textRef} {properties} \"locale\": \"{language}\", \"displayName\": \"{name}\", \"description\": \"{description}\" }}";
        }

        private string GetUpdateEndpointPostJson()
        {
            var name = _values.GetOrEmpty("csr.endpoint.name");
            var description = _values.GetOrEmpty("csr.endpoint.description");
            var properties = GetProperties("endpoint");

            var region = _values.GetOrEmpty("service.config.region");
            var projectId = _values.GetOrEmpty("csr.project.id");
            var projectUrl = GetCustomSpeechUrl(region, "projects", projectId);
            var projectRef = !string.IsNullOrEmpty(projectId) ? $"\"project\": {{ \"self\": \"{projectUrl}\" }}," : "";

            return $"{{ {projectRef} {properties} \"displayName\": \"{name}\", \"description\": \"{description}\" }}";
        }

        private string GetProperties(string thing)
        {
            var loggingEnabled = _values.GetOrDefault("service.config.content.logging.enabled", "false");
            var loggingPart = thing == "endpoint" ? $"\"contentLoggingEnabled\": {loggingEnabled}" : "";
            
            return $"\"properties\": {{ {loggingPart} }},";
        }

        private HttpWebRequest CreateWebRequest(string method, string path, string? id = null, string? query = null, string? contentType = null)
        {
            var key = _values.GetOrEmpty("service.config.key");
            var region = _values.GetOrEmpty("service.config.region");
            var timeout = _values.GetOrDefault("csr.wait.timeout", 100000);

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

        private string GetCustomSpeechUrl(string region, string path, string? id = null, string? query = null)
        {
            if (path.StartsWith("http")) return path;

            var idIsUrl = id != null && id.StartsWith("http");
            if (idIsUrl && id.Contains(path)) return id;

            var version = _values.GetOrDefault("csr.api.version", "v3.1");
            var pathPart = string.IsNullOrEmpty(id) ? path : $"{path}/{id}";
            var queryPart = string.IsNullOrEmpty(query) ? "" : $"?{query}";

            var endpoint = $"https://{region}.api.cognitive.microsoft.com/speechtotext/{version}";
            endpoint = _values.GetOrDefault("csr.api.endpoint", endpoint);

            return $"{endpoint}/{pathPart}{queryPart}";
        }

        private string CheckWriteOutputRequest(HttpWebRequest request, string payload = null, bool append = false)
        {
            var output = _values.GetOrEmpty("csr.output.request.file");
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

        private string? DownloadUrl(string url, string? defaultFileName = null)
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

        private string? ReadWritePrintResponse(HttpWebResponse response, string? defaultFileName = null)
        {
            var saveAs = HttpHelpers.GetOutputDataFileName(defaultFileName, response, _values, "csr", out _, out bool isJson);

            var message = !_quiet ? "Saving as" : null;
            var printJson = !_quiet && _verbose && isJson;
            var downloaded = HttpHelpers.ReadWriteResponse(response, saveAs, message, printJson);

            if (printJson) JsonHelpers.PrintJson(downloaded);

            return downloaded;
        }

        private string ReadWritePrintJson(HttpWebResponse response, bool skipWrite = false)
        {
            var json = HttpHelpers.ReadWriteJson(response, _values, "csr", skipWrite);
            if (!_quiet && _verbose) JsonHelpers.PrintJson(json);
            return json;
        }

        private bool _quiet = false;
        private bool _verbose = false;
    }
}
