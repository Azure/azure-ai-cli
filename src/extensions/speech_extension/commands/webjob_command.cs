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

namespace Azure.AI.Details.Common.CLI
{
    public class WebJobCommand : Command
    {
        internal WebJobCommand(ICommandValues values) : base(values)
        {
            _quiet = _values.GetOrDefault("x.quiet", false);
            _verbose = _values.GetOrDefault("x.verbose", true);
        }

        internal bool RunCommand()
        {
            try
            {
                RunWebJobCommand();
            }
            catch (WebException ex)
            {
                FileHelpers.LogException(_values, ex);
                ConsoleHelpers.WriteLineError($"\n  ERROR: {ex.Message}");
                JsonHelpers.PrintJson(HttpHelpers.ReadWriteJson(ex.Response, _values, "csr"));
            }

            return _values.GetOrDefault("passed", true);
        }

        private bool RunWebJobCommand()
        {
            DoCommand(_values.GetCommand());
            return _values.GetOrDefault("passed", true);
        }

        private void DoCommand(string command)
        {
            CheckPath();

            switch (command)
            {
                case "webjob.list": DoList(); break;
                case "webjob.upload": DoUpload(); break;
                case "webjob.run": DoRun(); break;
                case "webjob.status": DoStatus(); break;
                case "webjob.download": DoDownload(); break;
                case "webjob.delete": DoDelete(); break;
            }
        }

        /// <summary>
        ///   The `spx webjob list` command lists details about existing Azure
        ///   Triggered WebJobs used to remotely execute SPX zip packages.
        /// </summary>
        /// <example>
        ///   spx webjob list
        ///     --output request FILE/-
        ///     --output json FILE/-
        /// </example>
        private void DoList()
        {
            GetListParameters(out string path, out string name, out string nameAction, out string id, out string message, out string arrayName, out string urlName, out IdKind kind);

            if (!_quiet) Console.WriteLine(message);

            var request = CreateWebRequest(WebRequestMethods.Http.Get, path, name, nameAction, id);
            var payload = CheckWriteOutputRequest(request);
            var response = HttpHelpers.GetWebResponse(request, payload);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            var json = ReadWritePrintJson(response);
            CheckWriteOutputUrlsOrIds(json, arrayName, urlName, _values, "webjob", kind);
        }

        /// <summary>
        ///   The `spx webjob upload` command creates or updates an Azure
        ///   Triggered WebJob with the requested SPX zip package.
        /// </summary>
        /// <example>
        ///   spx recognize --file https://crbn.us/hello.wav --zip target webjob --zip FILE
        ///   spx webjob upload
        ///     --file FILE/-
        ///     --name NAME
        ///     --run
        ///     --output request FILE/-
        ///     --output json FILE/-
        /// </example>
        private void DoUpload()
        {
            GetUploadParameters(out string message, out string path, out string name, out string file, out string contentType);

            if (!_quiet) Console.WriteLine(message);

            var bytes = FileHelpers.ReadAllBytes(file);
            var fileName = (new FileInfo(file)).Name;

            var request = CreateWebRequest(WebRequestMethods.Http.Put, path, name, contentType: contentType);
            request.Headers.Add("Content-Disposition", $"attachment; filename=\"{fileName}\"");

            var payload = CheckWriteOutputRequest(request, bytes);
            var response = HttpHelpers.GetWebResponse(request, payload);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");
            
            var json = ReadWritePrintJson(response);
            UrlHelpers.CheckWriteOutputUrlOrId(json, "url", _values, "webjob", IdKind.FlatDateTime);

            var runJob = _values.GetOrDefault("webjob.run.job", false);
            if (runJob) DoRun(name);
        }

        /// <summary>
        ///   The `spx webjob run` command triggers an existing Azure Triggered
        ///   WebJob, and starts running the uploaded SPX zip package.
        /// </summary>
        /// <example>
        ///   spx webjob run
        ///     --name NAME
        ///     --output request FILE/-
        ///     --output json FILE/-
        /// </example>
        private void DoRun(string name = "")
        {
            GetRunParameters(out string path, ref name, out string nameAction, out string message);

            if (!_quiet) Console.WriteLine(message);

            var request = CreateWebRequest(WebRequestMethods.Http.Post, path, name, nameAction);
            var payload = CheckWriteOutputRequest(request, new byte[] { (int)'\n' });
            var response = HttpHelpers.GetWebResponse(request, payload);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            ReadWritePrintJson(response);

            var location = response.Headers["Location"];
            var id = location[(location.LastIndexOf('/') + 1)..];
            DoStatus(name, id);
        }

        /// <summary>
        ///   The `spx webjob status` command checks the status of existing
        ///   Azure Triggered WebJobs, providing additional details.
        /// </summary>
        /// <example>
        ///   spx webjob status
        ///     --name NAME
        ///     --id ID
        ///     --output request FILE/-
        ///     --output json FILE/-
        /// </example>
        private void DoStatus(string? name = null, string? id = null)
        {
            GetStatusParameters(out string path, ref name, out string nameAction, ref id, out string message);

            if (!_quiet) Console.WriteLine(message);

            var request = CreateWebRequest(WebRequestMethods.Http.Get, path, name, nameAction, id);
            var payload = CheckWriteOutputRequest(request);
            var response = HttpHelpers.GetWebResponse(request, payload);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            var json = ReadWritePrintJson(response);
            UrlHelpers.CheckWriteOutputUrlOrId(json, "url", _values, "webjob", IdKind.FlatDateTime);

            HttpHelpers.CheckWaitForComplete(response, json, () => CreateWebRequest(WebRequestMethods.Http.Get, path, name, nameAction, id), _values, "webjob", "Awaiting WebJob run success or failure ...", _quiet, _verbose);
        }

        /// <summary>
        ///   The `spx webjob download` command downloads files from Azure
        ///   Triggered WebJob runs that have already completed.
        /// </summary>
        /// <example>
        ///   spx webjob download
        ///     --name NAME
        ///     --output request FILE/-
        ///     --output json FILE/-
        /// </example>
        /// <example>
        ///   spx webjob download
        ///     --name NAME
        ///     --id ID
        ///     --output request FILE/-
        ///     --output json FILE/-
        /// </example>
        /// <example>
        ///   spx webjob download
        ///     --name NAME
        ///     --id ID
        ///     --file FILE(e.g. output_log.txt)
        ///     --output request FILE/-
        ///     --output file FILE/-
        /// </example>
        /// <example>
        ///   spx webjob download
        ///     --output request FILE/-
        ///     --output json FILE/-
        /// </example>
        /// <example>
        ///   spx webjob download
        ///     --file FILE(e.g. "data/" or "data/jobs/triggered/test237/202112160018125977/output_log.txt")
        ///     --output request FILE/-
        ///     --output json FILE/-
        /// </example>
        /// <example>
        ///   spx webjob download
        ///     --url URL(e.g. "https://robch-skyman.scm.azurewebsites.net/vfs/data/jobs/triggered/test237/202112160018125977/output_log.txt")
        ///     --output request FILE/-
        ///     --output file FILE/-
        /// </example>
        private string? DoDownload()
        {
            GetDownloadParameters(out string url, out string message);

            if (!_quiet) Console.WriteLine(message);

            var request = CreateWebRequest(WebRequestMethods.Http.Get, url);
            var payload = CheckWriteOutputRequest(request);
            var response = HttpHelpers.GetWebResponse(request, payload);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            return ReadWritePrintResponse(response);
        }

        /// <summary>
        ///   The `spx webjob delete` command deletes an existing
        ///   Azure Triggered WebJob.
        /// </summary>
        /// <example>
        ///   spx webjob delete
        ///     --name NAME
        ///     --output request FILE/-
        ///     --output json FILE/-
        /// </example>
        private void DoDelete()
        {
            GetDeleteParameters(out string path, out string name, out string nameAction, out string message);

            if (!_quiet) Console.WriteLine(message);

            var request = CreateWebRequest("DELETE", path, name, nameAction);
            var payload = CheckWriteOutputRequest(request);
            var response = HttpHelpers.GetWebResponse(request, payload);

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            ReadWritePrintJson(response);
        }

        private void GetListParameters(out string path, out string name, out string nameAction, out string id, out string message, out string arrayName, out string urlName, out IdKind kind)
        {
            name = _values.GetOrEmpty("webjob.job.name");
            id = _values.GetOrEmpty("webjob.job.id");

            var nameOk = !string.IsNullOrEmpty(name);
            var idOk = nameOk && !string.IsNullOrEmpty(id);
            id = idOk ? id.Trim('/') + '/' : ""; // the '/vfs/' apis require a trailing slash on directories, and we're listing files, so, it's a directory

            var listingJobs = !nameOk && !idOk;
            var listingHistory = nameOk && !idOk;
            var listingFiles = nameOk && idOk;

            nameAction = listingHistory ? "history" : "";
            arrayName = listingHistory ? "runs" : null;
            urlName = listingFiles ? "href" : "url";
            kind = listingHistory ? IdKind.FlatDateTime : IdKind.Name;

            message = listingJobs
                ? "Listing jobs..."
                : listingHistory
                    ? "Listing history..."
                    : "List files...";

            path = listingFiles
                ? "/vfs/data/jobs/triggered"
                : "/api/triggeredwebjobs";

        }

        private void GetUploadParameters(out string message, out string path, out string jobName, out string fileName, out string contentType)
        {
            contentType = "application/zip";
            message = "Uploading webjob ...";
            path = $"api/triggeredwebjobs";

            jobName = _values.GetOrEmpty("webjob.job.name");
            fileName = _values.GetOrEmpty("webjob.upload.job.file");
            fileName = FileHelpers.DemandFindFileInDataPath(fileName, _values, "webjob upload");

            var fileNameOk = !string.IsNullOrEmpty(fileName);
            if (!fileNameOk)
            {
                _values.AddThrowError(
                    "WARNING:", $"Uploading file; file name required!",
                                "",
                        "USE:", $"{Program.Name} webjob upload --file FILENAME [...]",
                                "",
                        "SEE:", $"{Program.Name} help webjob upload");
            }

            var jobNameOk = !string.IsNullOrEmpty(jobName);
            if (!jobNameOk) jobName = new FileInfo(fileName).Name;
        }

        private void GetRunParameters(out string path, ref string name, out string nameAction, out string message)
        {
            name = DemandWebJobName(name);
            nameAction = "run";

            message = $"Triggering job {name} ...";
            path = $"api/triggeredwebjobs";
        }

        private void GetStatusParameters(out string path, ref string name, out string nameAction, ref string id, out string message)
        {
            nameAction = "history";
            name = DemandWebJobName(name);
            id = DemandWebJobId(id);

            message = $"Finding status for {name} {id} ...";
            path = $"api/triggeredwebjobs";
        }

        private void GetDownloadParameters(out string url, out string message)
        {
            var jobid = _values.GetOrEmpty("webjob.job.id");
            if (!StringHelpers.SplitNameValue(jobid, out string job, out string id))
            {
                job = "";
                id = jobid;
            }

            var name = _values.GetOrDefault("webjob.job.name", job);
            if (string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(id))
            {
                name = id;
                id = "";
            }

            var file = _values.GetOrEmpty("webjob.download.file");
            if (string.IsNullOrEmpty(file))
            {
                file = ".";
            }

            url = _values.GetOrDefault("webjob.download.url", file);
            if (!url.StartsWith("http"))
            {
                url = $"vfs/";
                if (!string.IsNullOrEmpty(name)) url = $"{url}data/jobs/triggered/{name}/";
                if (!string.IsNullOrEmpty(id)) url = $"{url}{id}/";
                if (!string.IsNullOrEmpty(file)) url = $"{url}{file}";
            }

            message = url[..Math.Min(100, url.Length)];
            message = url.Length > 100
                ? $"Downloading {message}... "
                : $"Downloading {message} ... ";
        }

        private void GetDeleteParameters(out string path, out string name, out string nameAction, out string message)
        {
            nameAction = null;
            name = DemandWebJobName();

            message = $"Deleting job {name} ...";
            path = $"api/triggeredwebjobs";
        }

        private string DemandWebJobName(string? name = null)
        {
            return _values.DemandGetOrDefault("webjob.job.name", name,
                ErrorHelpers.CreateMessage(
                    "WARNING:", $"Requires valid name.", "",
                        "USE:", $"{Program.Name} webjob [...] --name NAME"));
        }

        private string DemandWebJobId(string? id = null)
        {
            return _values.DemandGetOrDefault("webjob.job.id", id, 
                ErrorHelpers.CreateMessage(
                    "WARNING:", $"Requires valid ID.", "",
                        "USE:", $"{Program.Name} webjob [...] --id ID"));
        }

        private HttpWebRequest CreateWebRequest(string method, string path, string? name = null, string nameAction = null, string? id = null, string? contentType = null)
        {
            var endpoint = _values.GetOrEmpty("webjob.config.endpoint").Trim('\r', '\n');
            var userName = _values.GetOrEmpty("webjob.config.username").Trim('\r', '\n');
            var password = _values.GetOrEmpty("webjob.config.password").Trim('\r', '\n');
            var timeout = _values.GetOrDefault("webjob.timeout", 100000);

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
            {
                _values.AddThrowError("ERROR:", $"Creating request ({path}); requires valid endpoint, username, and password.");
            }

            string url = GetWebJobUrl(endpoint, path, name, nameAction, id);

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Credentials = new NetworkCredential(userName, password);
            request.PreAuthenticate = true;
            request.Method = method;
            request.Accept = "application/json";
            request.Timeout = timeout;

            if (!string.IsNullOrEmpty(contentType))
            {
                request.ContentType = contentType;
            }

            return request;
        }

        private string GetWebJobUrl(string endpoint, string path, string? name = null, string nameAction = null, string? id = null)
        {
            if (path.StartsWith("http")) return path;

            endpoint = !endpoint.StartsWith("http")
                ? $"https://{endpoint}.scm.azurewebsites.net"
                : endpoint.TrimEnd('/');

            var nameOk = !string.IsNullOrEmpty(name);
            var actionOk = !string.IsNullOrEmpty(nameAction);
            var idOk = !string.IsNullOrEmpty(id);

            var trimPath = nameOk || actionOk || idOk;
            var pathPart = trimPath ? path.Trim('/') : path;

            var namePart = nameOk ? $"/{name}" : "";
            var nameActionPart = actionOk ? $"/{nameAction}" : "";
            var idPart = idOk ? $"/{id}" : "";

            return $"{endpoint}/{pathPart}{namePart}{nameActionPart}{idPart}";
        }

        private string CheckWriteOutputRequest(HttpWebRequest request, string payload = null, bool append = false)
        {
            var output = _values.GetOrEmpty("webjob.output.request.file");
            if (!string.IsNullOrEmpty(output))
            {
                var fileName = FileHelpers.GetOutputDataFileName(output, _values);
                payload = HttpHelpers.WriteOutputRequest(request, fileName, payload, append);
            }
            return payload;
        }

        private byte[] CheckWriteOutputRequest(HttpWebRequest request, byte[] payload)
        {
            var output = _values.GetOrEmpty("webjob.output.request.file");
            if (!string.IsNullOrEmpty(output))
            {
                var fileName = FileHelpers.GetOutputDataFileName(output, _values);
                payload = HttpHelpers.WriteOutputRequest(request, fileName, payload);
            }
            return payload;
        }

        private string ReadWritePrintJson(HttpWebResponse response)
        {
            var json = HttpHelpers.ReadWriteJson(response, _values, "webjob");
            if (!_quiet && _verbose) JsonHelpers.PrintJson(json);
            return json;
        }

        private string? ReadWritePrintResponse(HttpWebResponse response, string defaultFileName = null)
        {
            var saveAs = HttpHelpers.GetOutputDataFileName(defaultFileName, response, _values, "webjob", out bool isText, out bool isJson);

            var message = !_quiet ? "Saving as" : null;
            var printResponse = !_quiet && _verbose && (isJson || isText);
            var downloaded = HttpHelpers.ReadWriteResponse(response, saveAs, message, printResponse);

            if (printResponse && isJson) JsonHelpers.PrintJson(downloaded);
            if (printResponse && isText) Console.WriteLine(downloaded);

            return downloaded;
        }

        public static void CheckWriteOutputUrlsOrIds(string json, string arrayName, string urlName, INamedValues values, string domain, IdKind kinds = IdKind.Guid)
        {
            var urls = new List<string>();
            var ids = new List<string>();
            UrlHelpers.GetUrlsOrIds(json, arrayName, urlName, urls, ids, kinds);

            urls.Reverse();
            ids.Reverse();
            UrlHelpers.CheckWriteOutputUrlsOrIds(values, domain, urls, ids, kinds);
        }

        private readonly bool _quiet = false;
        private readonly bool _verbose = false;
    }
}
