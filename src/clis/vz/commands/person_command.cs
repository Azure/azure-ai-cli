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
using Newtonsoft.Json.Linq;

namespace Azure.AI.Details.Common.CLI
{
    public class PersonCommand : Command
    {
        internal PersonCommand(ICommandValues values) : base(values)
        {
            _quiet = _values.GetOrDefault("x.quiet", false);
            _verbose = _values.GetOrDefault("x.verbose", true);
        }

        internal bool RunCommand()
        {
            try
            {
                RunPersonCommand();
            }
            catch (WebException ex)
            {
                FileHelpers.LogException(_values, ex);
                ConsoleHelpers.WriteLineError($"\n  ERROR: {ex.Message}");
                JsonHelpers.PrintJson(HttpHelpers.ReadWriteJson(ex.Response, _values, "person"));
            }

            return _values.GetOrDefault("passed", true);
        }

        private bool RunPersonCommand()
        {
            DoCommand(_values.GetCommand());
            return _values.GetOrDefault("passed", true);
        }

        private void DoCommand(string command)
        {
            CheckPath();

            switch (command)
            {
                case "person.group.create": DoCreatePersonGroup(); break;
                case "person.group.update": DoUpdatePersonGroup(); break;
                case "person.group.delete": DoDeletePersonGroup(); break;
                case "person.group.list": DoList("persongroups"); break;

                case "person.group.train": DoTrainPersonGroup(); break;
                case "person.group.status": DoTrainPersonGroupStatus(); break;

                case "person.create": DoCreatePerson(); break;
                case "person.update": DoUpdatePerson(); break;
                case "person.delete": DoDeletePerson(); break;
                case "person.list": DoList("persons"); break;

                case "person.face.add": DoAddPersonFace(); break;
                case "person.face.update": DoUpdatePersonFace(); break;
                case "person.face.delete": DoDeletePersonFace(); break;
                case "person.face.list": DoList("personfaces"); break;

                default:
                    _values.AddThrowError("WARNING:", $"'{command.Replace('.', ' ')}' NOT YET IMPLEMENTED!!");
                    break;
            }
        }

        private void DoList(string kind = "")
        {
            GetListParameters(kind, out string path, out string message, out string query, out string arrayName, out string idName);

            if (!_quiet) Console.WriteLine(message);

            var request = CreateWebRequest(WebRequestMethods.Http.Get, path, null, query);
            var payload = CheckWriteOutputRequest(request);
            var response = HttpHelpers.GetWebResponse(request, payload);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            var json = ReadWritePrintJson(response);
            UrlHelpers.CheckWriteOutputUrlsOrIds(json, arrayName, idName, _values, "person");
        }

        private void DoCreatePersonGroup()
        {
            GetGroupId(out string kind, out string id, false);
            if (string.IsNullOrEmpty(id)) id = Guid.NewGuid().ToString();

            var name = _values.GetOrEmpty("person.group.name");
            if (string.IsNullOrEmpty(name))
            {
                _values.AddThrowError(
                    "WARNING:", $"Cannot create {kind} person group; group name not specified!", "",
                        "USE:", $"{Program.Name} person group create --name NAME [...]");
            }

            var message = $"Creating {kind} person group '{name}' ...";
            if (!_quiet) Console.WriteLine(message);

            var request = CreateWebRequest(WebRequestMethods.Http.Put, $"{kind}persongroups", id);
            var payload = CheckWriteOutputRequest(request, GetCreatePersonGroupPutJson(kind, name));
            var response = HttpHelpers.GetWebResponse(request, payload);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            ReadWritePrintJson(response);
            IdHelpers.CheckWriteOutputNameOrId(id, _values, "person", IdKind.Id);
            IdHelpers.CheckWriteOutputNameOrId(id, _values, "person", IdKind.Name);
        }

        private void DoUpdatePersonGroup()
        {
            GetGroupId(out string kind, out string id);

            var message = $"Updating {kind} person group '{id}' ...";
            if (!_quiet) Console.WriteLine(message);

            var request = CreateWebRequest("PATCH", $"{kind}persongroups", id);
            var payload = CheckWriteOutputRequest(request, GetUpdatePersonGroupPostJson(kind));
            var response = HttpHelpers.GetWebResponse(request, payload);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            ReadWritePrintJson(response);
        }

        private void DoDeletePersonGroup()
        {
            GetGroupId(out string kind, out string id);

            var message = $"Deleting {kind} person group '{id}' ...";
            if (!_quiet) Console.WriteLine(message);

            var request = CreateWebRequest("DELETE", $"{kind}persongroups", id);
            var payload = CheckWriteOutputRequest(request);
            var response = HttpHelpers.GetWebResponse(request, payload);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            ReadWritePrintJson(response);
        }

        private void DoTrainPersonGroup()
        {
            GetGroupId(out string kind, out string id);

            var message = $"Starting {kind} person group training '{id}' ...";
            if (!_quiet) Console.WriteLine(message);

            var request = CreateWebRequest(WebRequestMethods.Http.Post, $"{kind}persongroups/{id}/train");
            var payload = CheckWriteOutputRequest(request);
            var response = HttpHelpers.GetWebResponse(request, payload);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            ReadWritePrintJson(response);
            WaitForPersonGroupTrainingComplete(kind, id);
        }

        private void DoTrainPersonGroupStatus()
        {
            GetGroupId(out string kind, out string id);

            var message = $"Getting status for {kind} person group training '{id}' ...";
            if (!_quiet) Console.WriteLine(message);

            var request = CreateWebRequest(WebRequestMethods.Http.Get, $"{kind}persongroups/{id}/training");
            var payload = CheckWriteOutputRequest(request);
            var response = HttpHelpers.GetWebResponse(request, payload);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            var json = ReadWritePrintJson(response);
            CheckWaitForPersonGroupTrainingComplete(json, kind, id);
        }

        private void DoCreatePerson()
        {
            GetGroupId(out string kind, out string id);

            var name = _values.GetOrEmpty("person.name");
            if (string.IsNullOrEmpty(name))
            {
                _values.AddThrowError(
                    "WARNING:", $"Cannot create {kind} person; person name not specified!", "",
                        "USE:", $"{Program.Name} person create --name NAME [...]");
            }

            var message = $"Creating {kind} person '{name}' ...";
            if (!_quiet) Console.WriteLine(message);

            var request = CreateWebRequest(WebRequestMethods.Http.Post, $"{kind}persongroups/{id}/persons");
            var payload = CheckWriteOutputRequest(request, GetCreatePersonPostJson(kind, name));
            var response = HttpHelpers.GetWebResponse(request, payload);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            var json = ReadWritePrintJson(response);
            IdHelpers.CheckWriteOutputNameOrId(json, "personId", _values, "person", IdKind.Id);
        }

        private void DoUpdatePerson()
        {
            GetGroupAndPersonIds(out string kind, out string groupId, out string id);

            var message = $"Updating {kind} person '{id}' ...";
            if (!_quiet) Console.WriteLine(message);

            var request = CreateWebRequest("PATCH", $"{kind}persongroups/{groupId}/persons", id);
            var payload = CheckWriteOutputRequest(request, GetUpdatePersonPostJson(kind));
            var response = HttpHelpers.GetWebResponse(request, payload);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            ReadWritePrintJson(response);
        }

        private void DoDeletePerson()
        {
            GetGroupAndPersonIds(out string kind, out string groupId, out string id);

            var message = $"Deleting {kind} person '{id}' ...";
            if (!_quiet) Console.WriteLine(message);

            var request = CreateWebRequest("DELETE", $"{kind}persongroups/{groupId}/persons", id);
            var payload = CheckWriteOutputRequest(request);
            var response = HttpHelpers.GetWebResponse(request, payload);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            ReadWritePrintJson(response);
        }

        private void DoAddPersonFace()
        {
            GetGroupAndPersonIds(out string kind, out string groupId, out string id);

            var message = $"Adding {kind} person face ('{id}') ...";
            if (!_quiet) Console.WriteLine(message);

            var file = _values.GetOrEmpty("vision.input.file");
            var existing = FileHelpers.FindFileInDataPath(file, _values);

            var query = GetAddPersonFaceQueryString(file, existing);
            var formData = GetAddPersonFacePostFormData(file, existing);
            var contentType = !string.IsNullOrEmpty(existing) ? "application/octet-stream" : null;

            var request = CreateWebRequest(WebRequestMethods.Http.Post, $"{kind}persongroups/{groupId}/persons/{id}/persistedfaces", null, query, contentType);
            var payload = CheckWriteOutputRequest(request, formData);

            var response = HttpHelpers.GetWebResponse(request, payload);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            var json = ReadWritePrintJson(response);
            IdHelpers.CheckWriteOutputNameOrId(json, "persistedFaceId", _values, "person", IdKind.Id);
        }

        private void DoUpdatePersonFace()
        {
            GetGroupPersonAndFaceIds(out string kind, out string groupId, out string personId, out string id);

            var message = $"Updating {kind} person face '{id}' ...";
            if (!_quiet) Console.WriteLine(message);

            var request = CreateWebRequest("PATCH", $"{kind}persongroups/{groupId}/persons/{personId}/persistedfaces", id);
            var payload = CheckWriteOutputRequest(request, GetUpdatePersonFacePostJson(kind));
            var response = HttpHelpers.GetWebResponse(request, payload);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            ReadWritePrintJson(response);
        }

        private void DoDeletePersonFace()
        {
            GetGroupPersonAndFaceIds(out string kind, out string groupId, out string personId, out string id);

            var message = $"Deleting {kind} person face '{id}' ...";
            if (!_quiet) Console.WriteLine(message);

            var request = CreateWebRequest("DELETE", $"{kind}persongroups/{groupId}/persons/{personId}/persistedfaces", id);
            var payload = CheckWriteOutputRequest(request);
            var response = HttpHelpers.GetWebResponse(request, payload);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            ReadWritePrintJson(response);
        }

        private void GetGroupId(out string kind, out string id, bool groupIdRequired = true)
        {
            GetIds(out kind, out id, out _, out _, groupIdRequired, false, false);
        }

        private void GetGroupAndPersonIds(out string kind, out string groupId, out string personId)
        {
            GetIds(out kind, out groupId, out personId, out _, true, true, false);
        }

        private void GetGroupPersonAndFaceIds(out string kind, out string groupId, out string personId, out string faceId)
        {
            GetIds(out kind, out groupId, out personId, out faceId, true, true, true);
        }

        private void GetIds(out string kind, out string groupId, out string personId, out string id, bool groupIdRequired, bool personIdRequired, bool faceIdRequired)
        {
            kind = _values.GetOrDefault("person.group.kind", "large");
            groupId = _values.GetOrEmpty("person.group.id");
            if (string.IsNullOrEmpty(groupId) && groupIdRequired)
            {
                _values.AddThrowError(
                    "WARNING:", $"Group id not specified!", "",
                        "USE:", $"{Program.Name} {_values.GetCommandForDisplay()} --group id ID [...]");
            }
            personId = _values.GetOrEmpty("person.id");
            if (string.IsNullOrEmpty(personId) && personIdRequired)
            {
                _values.AddThrowError(
                    "WARNING:", $"Person id not specified!", "",
                        "USE:", $"{Program.Name} {_values.GetCommandForDisplay()} --person id ID [...]");
            }
            id = _values.GetOrEmpty("person.face.id");
            if (string.IsNullOrEmpty(id) && faceIdRequired)
            {
                _values.AddThrowError(
                    "WARNING:", $"Face id not specified!", "",
                        "USE:", $"{Program.Name} {_values.GetCommandForDisplay()} --face id ID [...]");
            }
        }

        private void GetListParameters(string kind, out string path, out string message, out string query, out string arrayName, out string idName)
        {
            var groupKind = _values.GetOrDefault("person.group.kind", "large");
            if (groupKind != "large" && groupKind != "dynamic")
            {
                _values.AddThrowError(
                  "ERROR:", $"Unknown group kind '{groupKind}'!", "",
                    "USE:", $"{Program.Name} person [...] list --group kind large",
                            $"{Program.Name} person [...] list --group kind dynamic",
                    "SEE:", $"{Program.Name} help person group");
            }

            var groupId = _values.GetOrDefault("person.group.id", null);
            var personId = _values.GetOrDefault("person.id", null);

            var listPersonGroups = kind == "persongroups";
            var listPersons = kind == "persons";
            var listFaces = kind == "personfaces";

            if ((listPersons || listFaces) && string.IsNullOrEmpty(groupId) && groupKind == "large")
            {
                _values.AddThrowError(
                    "WARNING:", $"Cannot list {groupKind} {kind}; group id required!", "",
                        "USE:", $"{Program.Name} person [...] --group id ID",
                        "SEE:", $"{Program.Name} help person group id");
            }
            else if (listFaces && string.IsNullOrEmpty(personId))
            {
                _values.AddThrowError(
                    "WARNING:", $"Cannot list person faces; person id required!", "",
                        "USE:", $"{Program.Name} person [...] --person id ID",
                        "SEE:", $"{Program.Name} help person id");
            }

            path = "";
            message = "";
            arrayName = null;
            idName = null;

            if (listPersonGroups)
            {
                path = $"{groupKind}persongroups";
                message = $"Listing {groupKind} person groups ...";
                arrayName = null;
                idName = $"{groupKind}PersonGroupId";
            }
            else if (listPersons)
            {
                path = $"{groupKind}persongroups/{groupId}/persons";
                message = $"Listing {groupKind} person group persons ...";
                arrayName = groupKind == "large" ? null : "personIds";
                idName = groupKind == "large" ? "personId" : null;
            }
            else if (listFaces)
            {
                path = $"{groupKind}persongroups/{groupId}/persons/{personId}";
                message = $"Listing {groupKind} person group person faces ...";
                arrayName = groupKind == "large" ? "persistedFaceIds" : "personIds";
                idName = null;
            }
            else
            {
                _values.AddThrowError(
                    "WARNING:", $"Couldn't find resource type to list!", "",
                        "SEE:", $"{Program.Name} help person");
            }

            var top = _values.GetOrEmpty("person.top");
            var skip = _values.GetOrEmpty("person.skip");

            query = "";
            if (!string.IsNullOrEmpty(skip)) query += $"&skip={skip}";
            if (!string.IsNullOrEmpty(top)) query += $"&top={top}";
            query = query.Trim('&');
        }

        private string GetCreatePersonGroupPutJson(string groupKind, string name)
        {
            var sb = new StringBuilder();
            sb.Append(JsonHelpers.MakeStringMember("name", name));
            sb.Append(JsonHelpers.ContinueWithStringMemberOrEmpty("userData", _values, "person.group.user.data"));

            sb.Append(groupKind == "dynamic"
                ? JsonHelpers.ContinueWithStringArrayMemberOrEmpty("addPersonIds", _values, "person.group.add.person.id", "person.group.add.person.ids", ";\r\n")
                : JsonHelpers.ContinueWithStringMemberOrEmpty("recognitionModel", _values, "person.group.recognition.model"));

            return $"{{{sb.ToString().Trim(',', ' ')}}}";
        }

        private string GetUpdatePersonGroupPostJson(string groupKind)
        {
            var sb = new StringBuilder();
            sb.Append(JsonHelpers.ContinueWithStringMemberOrEmpty("name", _values, "person.group.name"));
            sb.Append(JsonHelpers.ContinueWithStringMemberOrEmpty("userData", _values, "person.group.user.data"));

            if (groupKind == "dynamic")
            {
                sb.Append(JsonHelpers.ContinueWithStringArrayMemberOrEmpty("addPersonIds", _values, "person.group.add.person.id", "person.group.add.person.ids", ";\r\n"));
                sb.Append(JsonHelpers.ContinueWithStringArrayMemberOrEmpty("removePersonIds", _values, "person.group.remove.person.id", "person.group.remove.person.ids", ";\r\n"));
            }

            return $"{{{sb.ToString().Trim(',', ' ')}}}";
        }

        private string GetCreatePersonPostJson(string groupKind, string name)
        {
            var sb = new StringBuilder();
            sb.Append(JsonHelpers.MakeStringMember("name", name));
            sb.Append(JsonHelpers.ContinueWithStringMemberOrEmpty("userData", _values, "person.user.data"));

            return $"{{{sb.ToString().Trim(',', ' ')}}}";
        }

        private string GetUpdatePersonPostJson(string groupKind)
        {
            var sb = new StringBuilder();
            sb.Append(JsonHelpers.ContinueWithStringMemberOrEmpty("name", _values, "person.name"));
            sb.Append(JsonHelpers.ContinueWithStringMemberOrEmpty("userData", _values, "person.user.data"));

            return $"{{{sb.ToString().Trim(',', ' ')}}}";
        }

        private string GetAddPersonFaceQueryString(string file, string existing)
        {
            var overload = !string.IsNullOrEmpty(existing) ? "overload=stream" : "";

            var sb = new StringBuilder(overload);
            sb.Append(UrlHelpers.ContinueWithQueryStringOrEmpty("detectionModel", _values, "face.detection.model"));
            sb.Append(UrlHelpers.ContinueWithQueryStringOrEmpty("targetFace", _values, "person.face.target.rect"));
            sb.Append(UrlHelpers.ContinueWithQueryStringOrEmpty("userData", _values, "person.face.user.data"));

            return sb.Length > 0
                ? sb.ToString().TrimStart('&')
                : "";
        }

        private byte[] GetAddPersonFacePostFormData(string file, string existing)
        {
            return file.StartsWith("http") && string.IsNullOrEmpty(existing)
                ? Encoding.UTF8.GetBytes($"{{\"url\":\"{file}\"}}")
                : FileHelpers.ReadAllBytes(existing);
        }

        private string GetUpdatePersonFacePostJson(string groupKind)
        {
            var sb = new StringBuilder();
            sb.Append(JsonHelpers.ContinueWithStringMemberOrEmpty("userData", _values, "person.face.user.data"));
            return $"{{{sb.ToString().Trim(',', ' ')}}}";
        }

        private HttpWebRequest CreateWebRequest(string method, string path, string? id = null, string? query = null, string? contentType = null)
        {
            var key = _values.GetOrEmpty("service.config.key");
            var region = _values.GetOrEmpty("service.config.region");
            var timeout = _values.GetOrDefault("person.wait.timeout", 100000);

            if (string.IsNullOrEmpty(region) || string.IsNullOrEmpty(key))
            {
                _values.AddThrowError("ERROR:", $"Creating request ({path}); requires valid region and key.");
            }

            string url = GetFaceApiUrl(region, path, id, query);
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

        private string GetFaceApiUrl(string region, string path, string? id = null, string? query = null)
        {
            if (path.StartsWith("http")) return path;

            var idIsUrl = id != null && id.StartsWith("http");
            if (idIsUrl && id.Contains(path)) return id;

            var version = _values.GetOrDefault("face.api.version", "v1.0-preview");
            var pathPart = string.IsNullOrEmpty(id) ? path : $"{path}/{id}";
            var queryPart = string.IsNullOrEmpty(query) ? "" : $"?{query}";

            var endpoint = $"https://{region}.api.cognitive.microsoft.com/face/{version}";
            endpoint = _values.GetOrDefault("face.api.endpoint", endpoint);

            return $"{endpoint}/{pathPart}{queryPart}";
        }

        private string CheckWriteOutputRequest(HttpWebRequest request, string payload = null, bool append = false)
        {
            var output = _values.GetOrEmpty("person.output.request.file");
            if (!string.IsNullOrEmpty(output))
            {
                var fileName = FileHelpers.GetOutputDataFileName(output, _values);
                payload = HttpHelpers.WriteOutputRequest(request, fileName, payload, append);
            }
            return payload;
        }

        private byte[] CheckWriteOutputRequest(HttpWebRequest request, byte[] payload)
        {
            var output = _values.GetOrEmpty("person.output.request.file");
            if (!string.IsNullOrEmpty(output))
            {
                var fileName = FileHelpers.GetOutputDataFileName(output, _values);
                payload = HttpHelpers.WriteOutputRequest(request, fileName, payload);
            }
            return payload;
        }

        private bool CheckWaitForPersonGroupTrainingComplete(string statusJson, string kind, string id)
        {
            string path = $"{kind}persongroups/{id}/training";
            string message = $"Awaiting {kind} person group training success or failure '{id}' ...";
            HttpWebRequest createWebRequest() => CreateWebRequest(WebRequestMethods.Http.Get, path);
            return HttpHelpers.CheckWaitForComplete(null, statusJson, createWebRequest, _values, "person", message, _quiet, _verbose);
        }

        private bool WaitForPersonGroupTrainingComplete(string kind, string id)
        {
            string path = $"{kind}persongroups/{id}/training";
            string message = $"Awaiting {kind} person group training success or failure '{id}' ...";
            HttpWebRequest createWebRequest() => CreateWebRequest(WebRequestMethods.Http.Get, path);
            return HttpHelpers.WaitForComplete(createWebRequest, _values, "person", message, _quiet, _verbose);
        }

        private string ReadWritePrintJson(HttpWebResponse response, bool skipWrite = false)
        {
            var json = HttpHelpers.ReadWriteJson(response, _values, "person", skipWrite);
            if (!_quiet && _verbose) JsonHelpers.PrintJson(json);
            return json;
        }

        private bool _quiet = false;
        private bool _verbose = false;
    }
}
