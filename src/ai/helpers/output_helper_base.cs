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
using Microsoft.CognitiveServices.Speech.Intent;
using Microsoft.CognitiveServices.Speech.Translation;
using Microsoft.CognitiveServices.Speech.Transcription;
using System.Collections.Generic;
using System.IO.Compression;
using System.Globalization;
using System.Net;
using DevLab.JmesPath;

namespace Azure.AI.Details.Common.CLI
{
    public class OutputHelperBase
    {
        public OutputHelperBase(ICommandValues values)
        {
            _values = values;
        }

        public void StartOutput()
        {
            lock (this)
            {
                _overwriteEach = true;
                _outputEach = GetOutputEachColumns().Count() > 0;
                _outputEachFileName = FileHelpers.GetOutputDataFileName(GetOutputEachFileName(), _values);
                _outputEachFileType = GetOutputEachFileType();

                _outputAll = GetOutputAllColumns().Count() > 0;
                _outputAllFileName = FileHelpers.GetOutputDataFileName(GetOutputAllFileName(), _values);
                _outputAllFileType = GetOutputAllFileType();

                _lock.StartLock();
            }
        }

        public void StopOutput()
        {
            lock (this)
            {
                FlushOutputEachCacheStage2(_overwriteEach);
                _overwriteEach = false;

                FlushOutputAllCache();
                FlushOutputPropertyCache();
                FlushOutputZipFile();
                _lock!.StopLock();
            }
        }

        private bool ShouldCacheProperty()
        {
            return false; // TODO
        }

        public void EnsureCacheProperty(string name, string? value)
        {
            if (ShouldCacheProperty() && value != null) CacheProperty(name, value);
        }

        private void CacheProperty(string name, string value)
        {
            EnsureInitPropertyCache();
            _propertyCache!.Add(name, value);
        }

        private void EnsureInitPropertyCache()
        {
            if (_propertyCache == null)
            {
                _propertyCache = new Dictionary<string, string>();
            }
        }

        private void FlushOutputPropertyCache()
        {
            _propertyCache = null;
        }

        private bool ShouldOutputAll(string name)
        {
            var key = "output.all." + name;
            var exists = _values.Contains(key);
            if (exists) return _values.GetOrDefault(key, false);

            var columns = GetOutputAllColumns();
            var should = columns.Contains(name);
            _values.Add(key, should ? "true" : "false");

            return should;
        }

        public void EnsureCacheAll(string name, string value)
        {
            EnsureOutputAll(name, value);
        }

        public void EnsureCacheAll(string name, string format, params object[] arg)
        {
            EnsureOutputAll(name, format, arg);
        }

        private void EnsureCacheAll(char ch, string name, string format, params object[] arg)
        {
            EnsureOutputAll(ch, name, format, arg);
        }

        public void EnsureOutputAll(string name, string format, params object[] arg)
        {
            EnsureOutputAll('\n', name, format, arg);
        }

        public void EnsureOutputAll(string name, string value)
        {
            EnsureOutputAll('\n', name, "{0}", value);
        }

        public string? GetAllOutput(string name, string? defaultValue = null)
        {
            if (_outputAllCache == null || !_outputAllCache.ContainsKey(name)) return defaultValue;
            
            var sb = new StringBuilder();
            foreach (var item in _outputAllCache[name])
            {
                if (sb.Length > 0)
                {
                    sb.Append(_outputAllSeparatorCache[name]);
                }
                sb.Append(item);
            }

            return sb.ToString();
        }

        private void EnsureOutputAll(char ch, string name, string format, params object[] arg)
        {
            bool output = ShouldOutputAll(name);
            if (output) AppendOutputAll(ch, name, string.Format(format, arg));
        }

        private void AppendOutputAll(char ch, string name, string value)
        {
            EnsureInitOutputAllCache(name);
            _outputAllCache[name].Add(value);
            _outputAllSeparatorCache[name] = ch;
        }

        private void EnsureInitOutputAllCache(string name = null)
        {
            if (_outputAllCache == null)
            {
                _outputAllCache = new Dictionary<string, List<string>>();
                _outputAllSeparatorCache = new Dictionary<string, char>();
            }

            if (!_outputAllCache.ContainsKey(name))
            {
                _outputAllCache[name] = new List<string>();
            }
        }

        private void FlushOutputAllCache()
        {
            if (!_outputAll) return;

            var overwrite = _values.GetOrDefault("output.overwrite", false);
            if (overwrite) File.Delete(_outputAllFileName);

            switch (_outputAllFileType)
            {
                case "json":
                    OutputAllJsonFile();
                    break;

                case "tsv":
                    OutputAllTsvFile();
                    break;
            }

            _outputAllCache = null;
            _outputAllSeparatorCache = null;
        }

        private string GetOutputAllFileName()
        {
            var file = _values.GetOrDefault("output.all.file.name", "output.{run.time}." + GetOutputAllFileType());

            var id = _values.GetOrEmpty("audio.input.id");
            if (file.Contains("{id}")) file = file.Replace("{id}", id);

            var pid = Process.GetCurrentProcess().Id.ToString();
            if (file.Contains("{pid}")) file = file.Replace("{pid}", pid);

            var time = DateTime.Now.ToFileTime().ToString();
            if (file.Contains("{time}")) file = file.Replace("{time}", time);

            var runTime = _values.GetOrEmpty("x.run.time");
            if (file.Contains("{run.time}")) file = file.Replace("{run.time}", runTime);

            return file.ReplaceValues(_values);
        }

        private string GetOutputAllFileType()
        {
            return _values.GetOrDefault("output.all.file.type", "tsv");
        }

        private string[] GetOutputAllColumns()
        {
            bool hasColumns = _values.Contains("output.all.tsv.file.columns");
            if (hasColumns) return _values.Get("output.all.tsv.file.columns").Split(';', '\r', '\n', '\t');

            var output = _values.Names.Where(x => x.StartsWith("output.all.") && !x.Contains(".tsv.") && _values.GetOrDefault(x, false));
            return output.Select(x => x.Remove(0, "output.all.".Length)).ToArray();
        }

        private void OutputAllJsonFile()
        {
            var json = JsonHelpers.GetJsonObjectText(_outputAllCache) + Environment.NewLine;
            FileHelpers.AppendAllText(_outputAllFileName, json, Encoding.UTF8);
        }

        private void OutputAllTsvFile()
        {
            var columns = GetOutputAllColumns();
            EnsureOutputAllTsvFileHeader(_outputAllFileName, columns);
            OutputAllTsvFileRow(_outputAllFileName, columns);
        }

        private void EnsureOutputAllTsvFileHeader(string file, string[] columns)
        {
            var hasHeader = _values.GetOrDefault("output.all.tsv.file.has.header", true);
            if (hasHeader && !File.Exists(file) && columns.Length > 0)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var column in columns)
                {
                    sb.Append(column);
                    sb.Append('\t');
                }
                FileHelpers.WriteAllText(file, sb.ToString().Trim('\t') + "\n", Encoding.UTF8);
            }
        }

        private void OutputAllTsvFileRow(string file, string[] columns)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var column in columns)
            {
                var value = GetAllOutput(column, "")!;
                sb.Append(EncodeOutputValue(value));
                sb.Append('\t');
            }

            var row = sb.ToString();
            row = row.Substring(0, row.Length - 1);

            if (!string.IsNullOrWhiteSpace(row))
            {
                FileHelpers.AppendAllText(file, row + "\n", Encoding.UTF8);
            }
        }

        private string EncodeOutputValue(string value)
        {
            value = value.TrimEnd('\r', '\n', '\t');
            value = value.Replace("\r", "\\r");
            value = value.Replace("\n", "\\n");
            value = value.Replace("\t", "\\t");
            return value;
        }

        private bool ShouldOutputEach(string name)
        {
            var key = "output.each." + name;
            var exists = _values.Contains(key);
            if (exists) return _values.GetOrDefault(key, false);

            var columns = GetOutputEachColumns();
            var should = columns.Contains(name);
            _values.Add(key, should ? "true" : "false");

            return should;
        }

        public void EnsureOutputEach(string name, string value)
        {
            EnsureOutputEach(name, "{0}", value);
        }

        public void EnsureOutputEach(string name, string format, params object[] arg)
        {
            bool output = ShouldOutputEach(name);
            if (output) AddOutputEachCache(name, string.Format(format, arg));
        }

        private void AddOutputEachCache(string name, string value)
        {
            EnsureInitOutputEachCache();
            _outputEachCache![name] = value;
        }

        private void EnsureInitOutputEachCache()
        {
            if (_outputEachCache == null)
            {
                _outputEachCache = new Dictionary<string, string>();
            }

            if (_outputEachCache2 == null)
            {
                _outputEachCache2 = new List<Dictionary<string, string>>();
            }
        }

        private void FlushOutputEachCacheStage1()
        {
            if (!_outputEach) return;
            if (_outputEachCache == null) return;
            if (_outputEachCache2 == null) return;

            _outputEachCache2.Add(_outputEachCache);
            _outputEachCache = null;
        }

        private void FlushOutputEachCacheStage2(bool overwrite = false)
        {
            if (!_outputEach) return;

            overwrite = overwrite && _values.GetOrDefault("output.overwrite", false);
            if (overwrite) File.Delete(_outputEachFileName!);

            switch (_outputEachFileType)
            {
                case "json":
                    OutputEachJsonFile();
                    break;

                case "tsv":
                    OutputEachTsvFile();
                    break;
            }

            _outputEachCache = null;
            _outputEachCache2 = null;
        }

        private string GetOutputEachFileName()
        {
            var file = _values.GetOrDefault("output.each.file.name", "each.{run.time}." + GetOutputEachFileType());

            var id = _values.GetOrEmpty("audio.input.id");
            if (file.Contains("{id}")) file = file.Replace("{id}", id);

            var pid = Process.GetCurrentProcess().Id.ToString();
            if (file.Contains("{pid}")) file = file.Replace("{pid}", pid);

            var time = DateTime.Now.ToFileTime().ToString();
            if (file.Contains("{time}")) file = file.Replace("{time}", time);

            var runTime = _values.GetOrEmpty("x.run.time");
            if (file.Contains("{run.time}")) file = file.Replace("{run.time}", runTime);

            return file.ReplaceValues(_values);
        }

        private string GetOutputEachFileType()
        {
            return _values.GetOrDefault("output.each.file.type", "tsv");
        }

        private string[] GetOutputEachColumns()
        {
            bool hasColumns = _values.Contains("output.each.tsv.file.columns");
            if (hasColumns) return _values.Get("output.each.tsv.file.columns").Split(';', '\r', '\n', '\t');

            var output = _values.Names.Where(x => x.StartsWith("output.each.") && !x.Contains(".tsv.") && _values.GetOrDefault(x, false));
            return output.Select(x => x.Remove(0, "output.each.".Length)).ToArray();
        }

        private void OutputEachJsonFile()
        {
            var json = JsonHelpers.GetJsonArrayText(_outputEachCache2!) + Environment.NewLine;
            FileHelpers.AppendAllText(_outputEachFileName!, json, Encoding.UTF8);
        }

        private void OutputEachTsvFile()
        {
            var columns = GetOutputEachColumns();
            EnsureOutputEachTsvFileHeader(_outputEachFileName!, columns);
            OutputEachTsvFileRow(_outputEachFileName!, columns);
        }

        private void EnsureOutputEachTsvFileHeader(string file, string[] columns)
        {
            var hasHeader = _values.GetOrDefault("output.each.tsv.file.has.header", true);
            if (hasHeader && !File.Exists(file) && columns.Length > 0)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var column in columns)
                {
                    sb.Append(column);
                    sb.Append('\t');
                }
                FileHelpers.WriteAllText(file, sb.ToString().Trim('\t') + "\n", Encoding.UTF8);
            }
        }

        private void OutputEachTsvFileRow(string file, string[] columns)
        {
            if (_outputEachCache2 == null) return;

            foreach (var item in _outputEachCache2.ToList())
            {
                StringBuilder sb = new StringBuilder();
                foreach (var column in columns)
                {
                    var value = item != null && item.ContainsKey(column)
                        ? item[column]
                        : "";
                    sb.Append(EncodeOutputValue(value));
                    sb.Append('\t');
                }

                var row = sb.ToString();
                row = row.Substring(0, row.Length - 1);

                if (!string.IsNullOrWhiteSpace(row))
                {
                    FileHelpers.AppendAllText(file, row + "\n", Encoding.UTF8);
                }
            }
        }

        private void FlushOutputZipFile()
        {
            if (!_outputAll && !_outputEach) return;

            var zipFileName = _values.GetOrEmpty("output.zip.file");
            if (string.IsNullOrEmpty(zipFileName)) return;

            TryCatchHelpers.TryCatchRetry(() =>
            {
                zipFileName = FileHelpers.GetOutputDataFileName(zipFileName);
                if (!zipFileName.EndsWith(".zip")) zipFileName = zipFileName + ".zip";

                var overwrite = _values.GetOrDefault("outputs.overwrite", false);
                if (overwrite && File.Exists(zipFileName)) File.Delete(zipFileName);

                using (var archive = ZipFile.Open(zipFileName, ZipArchiveMode.Update))
                {
                    if (_outputAll) AddToZip(archive, _outputAllFileName);
                    if (_outputEach) AddToZip(archive, _outputEachFileName);
                }
            });
        }

        private void AddToZip(ZipArchive zip, string file)
        {
            var name = (new FileInfo(file)).Name;

            var entry = zip.GetEntry(name);
            if (entry != null) entry.Delete();

            zip.CreateEntryFromFile(file, name);
        }

        private ICommandValues _values;

        private SpinLock _lock = new SpinLock();

        private Dictionary<string, string>? _propertyCache = null;

        private bool _outputAll = false;
        private string _outputAllFileName = null;
        private string _outputAllFileType = null;
        private Dictionary<string, List<string>> _outputAllCache;
        private Dictionary<string, char> _outputAllSeparatorCache;

        private bool _outputEach = false;
        private bool _overwriteEach = false;
        private string? _outputEachFileName = null;
        private string? _outputEachFileType = null;
        private Dictionary<string, string>? _outputEachCache;
        private List<Dictionary<string, string>>? _outputEachCache2;
    }
}
