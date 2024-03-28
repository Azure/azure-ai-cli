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
using System.Text.Unicode;
using System.Text.Json;

namespace Azure.AI.Details.Common.CLI
{
    public class OutputHelper
    {
        public OutputHelper(ICommandValues values)
        {
            _values = values;
            _verbose = _values.GetOrDefault("x.verbose", false);
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

                _outputBatch = _values.GetOrDefault("output.batch.json", false);
                _outputBatchFileName = FileHelpers.GetOutputDataFileName(GetOutputBatchFileName(), _values);

                _outputSrt = _values.GetOrDefault("output.type.srt", false);
                _outputSrtFileName = FileHelpers.GetOutputDataFileName(GetOutputSrtFileName(), _values);

                _outputVtt = _values.GetOrDefault("output.type.vtt", false);
                _outputVttFileName = FileHelpers.GetOutputDataFileName(GetOutputVttFileName(), _values);

                _cacheResults = _values.Names.Any(s => s.StartsWith("check.result")) || _values.Names.Any(s => s.StartsWith("check.jmes"));

                var source = _values.GetOrDefault("source.language.config", "en-US")!.Split(';')[0];
                _sourceCulture = new System.Globalization.CultureInfo(source);

                var target = _values.GetOrDefault("target.language.config", source);
                _targetCulture = new System.Globalization.CultureInfo(source);

                _lock.StartLock();
            }
        }

        public void StopOutput()
        {
            lock (this)
            {
                FlushOutputEachCacheStage2(_overwriteEach);
                _overwriteEach = false; // Only overwrite the "each" file on the first session started...

                FlushOutputAllCache();
                FlushOutputResultCache();
                FlushOutputZipFile();
                _lock!.StopLock();
            }
        }

        public void Connected(ConnectionEventArgs e)
        {
            _lock!.EnterReaderLock();
            OutputConnected(e);
            _lock!.ExitReaderLock();
        }

        public void Disconnected(ConnectionEventArgs e)
        {
            _lock!.EnterReaderLock();
            OutputDisconnected(e);
            _lock!.ExitReaderLock();
        }

        public void ConnectionMessageReceived(ConnectionMessageEventArgs e)
        {
            _lock!.EnterReaderLock();
            OutputConnectionMessageReceived(e);
            _lock!.ExitReaderLock();
        }

        public void SessionStarted(SessionEventArgs e)
        {
            _lock!.EnterReaderLock();
            OutputSessionStarted(e);
            _lock!.ExitReaderLock();
        }

        public void SessionStopped(SessionEventArgs e)
        {
            _lock!.EnterReaderLock();
            OutputSessionStopped(e);
            _lock!.ExitReaderLock();
        }

        public void Recognizing(SpeechRecognitionEventArgs e)
        {
            _lock!.EnterReaderLock();
            OutputRecognizing(e);
            _lock!.ExitReaderLock();
        }

        public void Recognized(SpeechRecognitionEventArgs e)
        {
            _lock!.EnterReaderLock();
            OutputRecognized(e);
            _lock!.ExitReaderLock();
        }

        public void Canceled(SpeechRecognitionCanceledEventArgs e)
        {
            _lock!.EnterReaderLock();
            OutputCanceled(e);
            _lock!.ExitReaderLock();
        }
        
        public void Recognizing(IntentRecognitionEventArgs e)
        {
            _lock!.EnterReaderLock();
            OutputRecognizing(e);
            _lock!.ExitReaderLock();
        }

        public void Recognized(IntentRecognitionEventArgs e)
        {
            _lock!.EnterReaderLock();
            OutputRecognized(e);
            _lock!.ExitReaderLock();
        }       

        public void Canceled(IntentRecognitionCanceledEventArgs e)
        {
            _lock!.EnterReaderLock();
            OutputCanceled(e);
            _lock!.ExitReaderLock();
        }

        public void Transcribing(MeetingTranscriptionEventArgs e)
        {
            _lock!.EnterReaderLock();
            OutputTranscribing(e);
            _lock!.ExitReaderLock();
        }

        public void Transcribed(MeetingTranscriptionEventArgs e)
        {
            _lock!.EnterReaderLock();
            OutputTranscribed(e);
            _lock!.ExitReaderLock();
        }

        public void Canceled(MeetingTranscriptionCanceledEventArgs e)
        {
            _lock!.EnterReaderLock();
            OutputCanceled(e);
            _lock!.ExitReaderLock();
        }

        public void Transcribing(ConversationTranscriptionEventArgs e)
        {
            _lock!.EnterReaderLock();
            OutputTranscribing(e);
            _lock!.ExitReaderLock();
        }

        public void Transcribed(ConversationTranscriptionEventArgs e)
        {
            _lock!.EnterReaderLock();
            OutputTranscribed(e);
            _lock!.ExitReaderLock();
        }

        public void Canceled(ConversationTranscriptionCanceledEventArgs e)
        {
            _lock!.EnterReaderLock();
            OutputCanceled(e);
            _lock!.ExitReaderLock();
        }

        public void Recognizing(TranslationRecognitionEventArgs e)
        {
            _lock!.EnterReaderLock();
            OutputRecognizing(e);
            _lock!.ExitReaderLock();
        }

        public void Recognized(TranslationRecognitionEventArgs e)
        {
            _lock!.EnterReaderLock();
            OutputRecognized(e);
            _lock!.ExitReaderLock();
        }

        public void Canceled(TranslationRecognitionCanceledEventArgs e)
        {
            _lock!.EnterReaderLock();
            OutputCanceled(e);
            _lock!.ExitReaderLock();
        }

        public void SynthesisStarted(SpeechSynthesisEventArgs e)
        {
            _lock!.EnterReaderLock();
            OutputSynthesisStarted(e);
            _lock!.ExitReaderLock();
        }

        public void Synthesizing(SpeechSynthesisEventArgs e)
        {
            _lock!.EnterReaderLock();
            OutputSynthesizing(e);
            _lock!.ExitReaderLock();
        }

        public void SynthesisCompleted(SpeechSynthesisEventArgs e)
        {
            _lock!.EnterReaderLock();
            OutputSynthesisCompleted(e);
            _lock!.ExitReaderLock();
        }

        public void SynthesisCanceled(SpeechSynthesisEventArgs e)
        {
            _lock!.EnterReaderLock();
            OutputSynthesisCanceled(e);
            _lock!.ExitReaderLock();
        }

        public void SynthesisWordBoundary(SpeechSynthesisWordBoundaryEventArgs e)
        {
            _lock!.EnterReaderLock();
            OutputWordBoundary(e);
            _lock!.ExitReaderLock();
        }

        public void EnsureCachePropertyCollection(string name, PropertyCollection properties)
        {
            EnsureInitPropertyCollectionCache();
            _propertyCollectionCache![name] = properties;
        }

        private void EnsureInitPropertyCollectionCache()
        {
            if (_propertyCollectionCache == null)
            {
                _propertyCollectionCache = new Dictionary<string, PropertyCollection>();
            }
        }

        private PropertyCollection? GetPropertyCollection(string name)
        {
            EnsureInitPropertyCollectionCache();
            return _propertyCollectionCache!.ContainsKey(name)
                ? _propertyCollectionCache[name]
                : null;
        }

        private bool ShouldCacheProperty()
        {
            return _outputBatch;
        }

        public void EnsureCacheProperty(string name, string? value)
        {
            if (ShouldCacheProperty() && value != null) CacheProperty(name, value);
        }

        private bool ShouldCacheOutputResult()
        {
            return _outputBatch || _outputSrt || _outputVtt || _cacheResults;
        }

        private void EnsureCacheOutputResult(SpeechRecognitionResult result)
        {
            if (ShouldCacheOutputResult()) CacheOutputResult(result);
        }

        private void EnsureCacheOutputResult(IntentRecognitionResult result)
        {
            if (ShouldCacheOutputResult()) CacheOutputResult(result);
        }

        private void EnsureCacheOutputResult(MeetingTranscriptionResult result)
        {
            if (ShouldCacheOutputResult()) CacheOutputResult(result);
        }

        private void EnsureCacheOutputResult(ConversationTranscriptionResult result)
        {
            if (ShouldCacheOutputResult()) CacheOutputResult(result);
        }

        private void EnsureCacheOutputResult(TranslationRecognitionResult result)
        {
            if (ShouldCacheOutputResult()) CacheOutputResult(result);
        }

        private void EnsureCacheOutputResult(SpeechSynthesisResult result)
        {
            if (ShouldCacheOutputResult()) CacheOutputResult(result);
        }

        private void CacheProperty(string name, string value)
        {
            EnsureInitOutputResultCache();
            _propertyCache!.Add(name, value);
        }

        private void CacheOutputResult(SpeechRecognitionResult result)
        {
            EnsureInitOutputResultCache();
            _outputResultCache!.Add(result);
        }

        private void CacheOutputResult(IntentRecognitionResult result)
        {
            EnsureInitOutputResultCache();
            _outputResultCache!.Add(result);
        }

        private void CacheOutputResult(MeetingTranscriptionResult result)
        {
            EnsureInitOutputResultCache();
            _outputResultCache!.Add(result);
        }

        private void CacheOutputResult(ConversationTranscriptionResult result)
        {
            EnsureInitOutputResultCache();
            _outputResultCache!.Add(result);
        }

        private void CacheOutputResult(TranslationRecognitionResult result)
        {
            EnsureInitOutputResultCache();
            _outputResultCache!.Add(result);
        }

        private void CacheOutputResult(SpeechSynthesisResult result)
        {
            EnsureInitOutputResultCache();
            _outputResultCache!.Add(result);
        }

        private void EnsureInitOutputResultCache()
        {
            if (_outputResultCache == null)
            {
                _outputResultCache = new List<object>();
            }

            if (_propertyCache == null)
            {
                _propertyCache = new Dictionary<string, string>();
            }
        }

        private void FlushOutputResultCache()
        {
            if (_outputBatch) OutputBatchJsonFile();
            if (_outputSrt) OutputSrtFile();
            if (_outputVtt) OutputVttFile();

            _outputResultCache = null;
            _propertyCache = null;
        }

        private string GetOutputBatchFileName()
        {
            var file = _values.GetOrDefault("output.batch.file.name", "output.{id}.{run.time}.json")!;

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

        private string GetOutputSrtFileName()
        {
            var file = _values.GetOrDefault("output.srt.file.name", "output.{id}.{run.time}.srt")!;

            var id = _values.GetOrEmpty("audio.input.id");
            if (file.Contains("{id}")) file = file.Replace("{id}", id);

            var runTime = _values.GetOrEmpty("x.run.time");
            if (file.Contains("{run.time}")) file = file.Replace("{run.time}", runTime);

            return file.ReplaceValues(_values);
        }

        private string GetOutputVttFileName()
        {
            var file = _values.GetOrDefault("output.vtt.file.name", "output.{id}.{run.time}.vtt")!;

            var id = _values.GetOrEmpty("audio.input.id");
            if (file.Contains("{id}")) file = file.Replace("{id}", id);

            var runTime = _values.GetOrEmpty("x.run.time");
            if (file.Contains("{run.time}")) file = file.Replace("{run.time}", runTime);

            return file.ReplaceValues(_values);
        }

        private void OutputBatchJsonFile()
        {
            var stream = new MemoryStream();
            var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
            writer.WriteStartObject();

            writer.WriteStartArray("AudioFileResults");
            WriteBatchAudioFileJsonObject(writer);
            writer.WriteEndArray();

            writer.WriteEndObject();
            writer.Flush();

            var text = Encoding.UTF8.GetString(stream.ToArray()) + Environment.NewLine;
            FileHelpers.WriteAllText(_outputBatchFileName!, text, Encoding.UTF8);
        }

        private void OutputSrtFile()
        {
            var text = GetSrtFile() + Environment.NewLine;
            FileHelpers.WriteAllText(_outputSrtFileName!, text, Encoding.UTF8);

            var languages = GetTargetLanguages();
            foreach (var language in languages)
            {
                var translated = GetSrtFile(language) + Environment.NewLine;
                var translatedFile = FileHelpers.AppendToFileName(_outputSrtFileName!, $".{language}", "");
                FileHelpers.WriteAllText(translatedFile, translated, Encoding.UTF8);
            }
        }

        private void OutputVttFile()
        { 
            var text = GetVttFile() + Environment.NewLine;
            FileHelpers.WriteAllText(_outputVttFileName!, text, Encoding.UTF8);

            var languages = GetTargetLanguages();
            foreach (var language in languages)
            {
                var translated = GetVttFile(language) + Environment.NewLine;
                var translatedFile = FileHelpers.AppendToFileName(_outputVttFileName!, $".{language}", "");
                FileHelpers.WriteAllText(translatedFile, translated, Encoding.UTF8);
            }
        }

        private string[] GetTargetLanguages()
        {
            var languages = _values.GetOrEmpty("target.language.config")?.Split(';', StringSplitOptions.RemoveEmptyEntries);
            return languages ?? Array.Empty<string>();
        }

        private void WriteBatchAudioFileJsonObject(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();

            var id = _propertyCache!["audio.input.id"];
            var file = _propertyCache["audio.input.file"];

            if (file != null && file.StartsWith("http"))
            {
                writer.WriteString("AudioFileName", id);
                writer.WriteString("AudioFileUrl", file);
            }
            else
            {
                var existing = FileHelpers.FindFileInDataPath(file!, _values);
                writer.WriteString("AudioFileName", file ?? id);
                writer.WriteString("AudioFileUrl", existing ?? file ?? id);
            }

            writer.WriteNumber("AudioLengthInSeconds", GetBatchAudioLengthInSeconds());

            WriteBatchAudioFileCombinedResultsJsonProperty(writer, "CombinedResults");
            WriteBatchAudioFileSegmentResultsJsonProperty(writer, "SegmentResults");

            writer.WriteEndObject();
        }

        private string GetSrtFile(string? language = null)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var caption in CaptionHelper.GetCaptions(language, 37, 3, _outputResultCache!))
            {
                sb.AppendLine($"{caption.Sequence}");
                sb.AppendLine($@"{caption.Begin:hh\:mm\:ss\,fff} --> {caption.End:hh\:mm\:ss\,fff}");
                sb.AppendLine(caption.Text);
                sb.AppendLine();
            }
            return sb.ToString().Trim();
        }

        private string GetVttFile(string? language = null)
        {
            StringBuilder sb = new StringBuilder($"WEBVTT{Environment.NewLine}");
            foreach (var caption in CaptionHelper.GetCaptions(language, 37, 3, _outputResultCache!))
            {
                sb.AppendLine();
                sb.AppendLine($@"{caption.Begin:hh\:mm\:ss\.fff} --> {caption.End:hh\:mm\:ss\.fff}");
                sb.AppendLine(caption.Text);
            }
            return sb.ToString().Trim();
        }

        private void WriteBatchAudioFileCombinedResultsJsonProperty(Utf8JsonWriter writer, string arrayName)
        {
            writer.WriteStartArray(arrayName);
            writer.WriteStartObject();

            writer.WriteString("ChannelNumber", "0");
            writer.WriteString("Lexical", GetBatchCombinedText("Lexical"));
            writer.WriteString("ITN", GetBatchCombinedText("ITN"));
            writer.WriteString("MaskedITN", GetBatchCombinedText("MaskedITN"));
            writer.WriteString("Display", GetBatchCombinedText("Display"));

            writer.WriteEndObject();
            writer.WriteEndArray();
        }

        private string GetBatchCombinedText(string kind)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var result in _outputResultCache!)
            {
                if (IsFinalResult(result))
                {
                    var text = GetResultText(result, kind);
                    sb.Append($" {text}");
                }
            }
            return sb.ToString().Trim();
        }

        private bool IsFinalResult(object result)
        {
            var final = result as RecognitionResult;
            return final?.Reason == ResultReason.RecognizedSpeech ||
                   final?.Reason == ResultReason.RecognizedIntent ||
                   final?.Reason == ResultReason.TranslatedSpeech;
        }

        private string GetResultText(object result, string kind)
        {
            var resultSpeech = result as SpeechRecognitionResult;
            var resultTranslation = result as TranslationRecognitionResult;
            var resultConversationTranscription = result as ConversationTranscriptionResult;
            var resultMeetingTranscription = result as MeetingTranscriptionResult;

            var text = resultSpeech != null
                ? resultSpeech.Properties.GetProperty(kind)
                : resultTranslation != null
                    ? resultTranslation.Properties.GetProperty(kind)
                    : resultConversationTranscription != null
                        ? resultConversationTranscription.Properties.GetProperty(kind)
                        : resultMeetingTranscription != null
                            ? resultMeetingTranscription.Properties.GetProperty(kind)
                            : "";

            return !string.IsNullOrEmpty(text)
                ? text
                : resultSpeech != null
                    ? resultSpeech.Text
                    : resultTranslation != null
                        ? resultTranslation.Text
                        : resultConversationTranscription != null
                            ? resultConversationTranscription.Text
                            : resultMeetingTranscription != null
                                ? resultMeetingTranscription.Text
                                : "";
        }

        private void WriteBatchAudioFileSegmentResultsJsonProperty(Utf8JsonWriter writer, string arrayName)
        {
            writer.WriteStartArray(arrayName);
            foreach (var result in _outputResultCache!)
            {
                WriteBatchAudioFileSegmentJsonObject(writer, result);
            }
            writer.WriteEndArray();
        }

        private bool WriteBatchAudioFileSegmentJsonObject(Utf8JsonWriter writer, object result)
        {
            if (!IsFinalResult(result)) return false;

            var resultSpeech = result as SpeechRecognitionResult;
            var resultTranslation = result as TranslationRecognitionResult;
            var resultConversationTranscription = result as ConversationTranscriptionResult;
            var resultMeetingTranscription = result as MeetingTranscriptionResult;

            return resultSpeech != null
                ? WriteBatchAudioFileSegmentJsonObject(writer, resultSpeech)
                : resultTranslation != null
                    ? WriteBatchAudioFileSegmentJsonObject(writer, resultTranslation)
                    : resultConversationTranscription != null
                        ? WriteBatchAudioFileSegmentJsonObject(writer, resultConversationTranscription)
                        : resultMeetingTranscription != null
                            ? WriteBatchAudioFileSegmentJsonObject(writer, resultMeetingTranscription)
                            : false;
        }

        private bool WriteBatchAudioFileSegmentJsonObject(Utf8JsonWriter writer, SpeechRecognitionResult result)
        {
            if (result.Reason == ResultReason.Canceled)
            {
                var cancelDetails = CancellationDetails.FromResult(result);
                if (cancelDetails.Reason == CancellationReason.EndOfStream) return false;

                writer.WriteStartObject();
                writer.WriteString("ErrorDetails", cancelDetails.ErrorDetails);
                writer.WriteString("ErrorCode", cancelDetails.ErrorCode.ToString());
                writer.WriteEndObject();
                return true;
            }

            writer.WriteStartObject();

            var json = result.Properties.GetProperty(PropertyId.SpeechServiceResponse_JsonResult);
            var parsed = !string.IsNullOrEmpty(json) ? JsonDocument.Parse(json) : null;

            var status = parsed?.GetPropertyStringOrNull("RecognitionStatus") ?? result.Reason.ToString();
            writer.WriteString("RecognitionStatus", status);

            writer.WriteString("ChannelNumber", "0");
            writer.WriteNull("SpeakerId");
            writer.WriteNumber("Offset", result.OffsetInTicks);
            writer.WriteNumber("Duration", (long)(result.Duration.TotalMilliseconds * 10000));
            writer.WriteNumber("OffsetInSeconds", Math.Round(1.0 * result.OffsetInTicks / 10000 / 1000, 2));
            writer.WriteNumber("DurationInSeconds", Math.Round(result.Duration.TotalSeconds, 2));

            writer.WritePropertyName("NBest");
            writer.WriteRawValue(parsed?.GetPropertyElementOrNull("NBest")?.GetRawText() ?? "[]");

            writer.WriteEndObject();
            return true;
        }

        private bool WriteBatchAudioFileSegmentJsonObject(Utf8JsonWriter writer, TranslationRecognitionResult result)
        {
            if (result.Reason == ResultReason.Canceled)
            {
                var cancelDetails = CancellationDetails.FromResult(result);
                if (cancelDetails.Reason == CancellationReason.EndOfStream) return false;

                writer.WriteStartObject();
                writer.WriteString("ErrorDetails", cancelDetails.ErrorDetails);
                writer.WriteString("ErrorCode", cancelDetails.ErrorCode.ToString());
                writer.WriteEndObject();
                return true;
            }

            var json = result.Properties.GetProperty(PropertyId.SpeechServiceResponse_JsonResult);
            var parsed = !string.IsNullOrEmpty(json) ? JsonDocument.Parse(json) : null;

            var status = parsed?.GetPropertyStringOrNull("RecognitionStatus") ?? result.Reason.ToString();
            writer.WriteString("RecognitionStatus", status);

            writer.WriteString("ChannelNumber", "0");
            writer.WriteNull("SpeakerId");
            writer.WriteNumber("Offset", result.OffsetInTicks);
            writer.WriteNumber("Duration", (long)(result.Duration.TotalMilliseconds * 10000));
            writer.WriteNumber("OffsetInSeconds", Math.Round(1.0 * result.OffsetInTicks / 10000 / 1000, 2));
            writer.WriteNumber("DurationInSeconds", Math.Round(result.Duration.TotalSeconds, 2));

            writer.WritePropertyName("NBest");
            writer.WriteRawValue(parsed?.GetPropertyElementOrNull("NBest")?.GetRawText() ?? "[]");

            writer.WriteEndObject();
            return true;
        }

        private bool WriteBatchAudioFileSegmentJsonObject(Utf8JsonWriter writer, ConversationTranscriptionResult result)
        {
            if (result.Reason == ResultReason.Canceled)
            {
                var cancelDetails = CancellationDetails.FromResult(result);
                if (cancelDetails.Reason == CancellationReason.EndOfStream) return false;

                writer.WriteStartObject();
                writer.WriteString("ErrorDetails", cancelDetails.ErrorDetails);
                writer.WriteString("ErrorCode", cancelDetails.ErrorCode.ToString());
                writer.WriteEndObject();
                return true;
            }

            var json = result.Properties.GetProperty(PropertyId.SpeechServiceResponse_JsonResult);
            var parsed = !string.IsNullOrEmpty(json) ? JsonDocument.Parse(json) : null;

            var status = parsed?.GetPropertyStringOrNull("RecognitionStatus") ?? result.Reason.ToString();
            writer.WriteString("RecognitionStatus", status);

            writer.WriteString("ChannelNumber", "0");
            writer.WriteNull("SpeakerId");
            writer.WriteNumber("Offset", result.OffsetInTicks);
            writer.WriteNumber("Duration", (long)(result.Duration.TotalMilliseconds * 10000));
            writer.WriteNumber("OffsetInSeconds", Math.Round(1.0 * result.OffsetInTicks / 10000 / 1000, 2));
            writer.WriteNumber("DurationInSeconds", Math.Round(result.Duration.TotalSeconds, 2));

            writer.WritePropertyName("NBest");
            writer.WriteRawValue(parsed?.GetPropertyElementOrNull("NBest")?.GetRawText() ?? "[]");

            writer.WriteEndObject();
            return true;
        }

        private bool WriteBatchAudioFileSegmentJsonObject(Utf8JsonWriter writer, MeetingTranscriptionResult result)
        {
            if (result.Reason == ResultReason.Canceled)
            {
                var cancelDetails = CancellationDetails.FromResult(result);
                if (cancelDetails.Reason == CancellationReason.EndOfStream) return false;

                writer.WriteStartObject();
                writer.WriteString("ErrorDetails", cancelDetails.ErrorDetails);
                writer.WriteString("ErrorCode", cancelDetails.ErrorCode.ToString());
                writer.WriteEndObject();
                return true;
            }

            var json = result.Properties.GetProperty(PropertyId.SpeechServiceResponse_JsonResult);
            var parsed = !string.IsNullOrEmpty(json) ? JsonDocument.Parse(json) : null;

            var status = parsed?.GetPropertyStringOrNull("RecognitionStatus") ?? result.Reason.ToString();
            writer.WriteString("RecognitionStatus", status);

            writer.WriteString("ChannelNumber", "0");
            writer.WriteNull("SpeakerId");
            writer.WriteNumber("Offset", result.OffsetInTicks);
            writer.WriteNumber("Duration", (long)(result.Duration.TotalMilliseconds * 10000));
            writer.WriteNumber("OffsetInSeconds", Math.Round(1.0 * result.OffsetInTicks / 10000 / 1000, 2));
            writer.WriteNumber("DurationInSeconds", Math.Round(result.Duration.TotalSeconds, 2));

            writer.WritePropertyName("NBest");
            writer.WriteRawValue(parsed?.GetPropertyElementOrNull("NBest")?.GetRawText() ?? "[]");

            writer.WriteEndObject();
            return true;
        }

        private double GetBatchAudioLengthInSeconds()
        {
            double length = 0;
            foreach (var result in _outputResultCache!)
            {
                double offset, duration;
                GetResultOffsetAndDuration(result, out offset, out duration);
                length = Math.Max(length, offset + duration);
            }

            return Math.Round(length, 2);
        }

        private void GetResultOffsetAndDuration(object result, out double offset, out double duration)
        {
            var resultSpeech = result as SpeechRecognitionResult;
            var resultTranslation = result as TranslationRecognitionResult;
            var resultConversationTranscription = result as ConversationTranscriptionResult;
            var resultMeetingTranscription = result as MeetingTranscriptionResult;

            offset = (resultSpeech != null
                ? resultSpeech.OffsetInTicks
                : resultTranslation != null
                    ? resultTranslation.OffsetInTicks
                    : resultConversationTranscription != null
                        ? resultConversationTranscription.OffsetInTicks
                        : resultMeetingTranscription != null
                            ? resultMeetingTranscription.OffsetInTicks
                            : 0) / 10000 / 1000;
            duration = (resultSpeech != null
                ? resultSpeech.Duration.TotalSeconds
                : resultTranslation != null
                    ? resultTranslation.Duration.TotalSeconds
                    : resultConversationTranscription != null
                        ? resultConversationTranscription.Duration.TotalSeconds
                        : resultMeetingTranscription != null
                            ? resultMeetingTranscription.Duration.TotalSeconds
                            : 0);
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

        private bool ShouldCheckAll()
        {
            var key = "check.jmes";
            var exists = _values.Contains(key);

            return exists;
        }

        public void EnsureCacheAll(string name, string value)
        {
            EnsureOutputAll(name, value);
            EnsureCheckAll(name, value);
        }

        public void EnsureCacheAll(string name, string format, params object[] arg)
        {
            EnsureOutputAll(name, format, arg);
            EnsureCheckAll(name, format, arg);
        }

        private void EnsureCacheAll(char ch, string name, string format, params object[] arg)
        {
            EnsureOutputAll(ch, name, format, arg);
            EnsureCheckAll(ch, name, format, arg);
        }

        private void EnsureCheckAll(char ch, string name, string format, params object[] arg)
        {
            bool output = ShouldCheckAll();
            if (output) AppendCheckAll(ch, name, string.Format(format, arg));
        }

        public void EnsureCheckAll(string name, string format, params object[] arg)
        {
            EnsureCheckAll('\n', name, format, arg);
        }

        public void EnsureCheckAll(string name, string value)
        {
            EnsureCheckAll('\n', name, "{0}", value);
        }

        private void AppendCheckAll(char ch, string name, string value)
        {
            EnsureInitCheckAllCache(name);
            _checkAllCache![name].Add(value);
        }

        private void EnsureInitCheckAllCache(string name)
        {
            if (_checkAllCache == null)
            {
                _checkAllCache = new Dictionary<string, List<string>>();
            }

            if (!_checkAllCache.ContainsKey(name))
            {
                _checkAllCache[name] = new List<string>();
            }
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
                    sb.Append(_outputAllSeparatorCache![name]);
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
            _outputAllCache![name].Add(value);
            _outputAllSeparatorCache![name] = ch;
        }

        private void EnsureInitOutputAllCache(string name)
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
            if (overwrite) File.Delete(_outputAllFileName!);

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
            var file = _values.GetOrDefault("output.all.file.name", "output.{run.time}." + GetOutputAllFileType())!;

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
            return _values.GetOrDefault("output.all.file.type", "tsv")!;
        }

        private string[] GetOutputAllColumns()
        {
            bool hasColumns = _values.Contains("output.all.tsv.file.columns");
            if (hasColumns) return _values.Get("output.all.tsv.file.columns")!.Split(';', '\r', '\n', '\t');

            var output = _values.Names.Where(x => x.StartsWith("output.all.") && !x.Contains(".tsv.") && _values.GetOrDefault(x, false));
            return output.Select(x => x.Remove(0, "output.all.".Length)).ToArray();
        }

        private void OutputAllJsonFile()
        {
            var json = JsonHelpers.GetJsonObjectText(_outputAllCache!) + Environment.NewLine;
            FileHelpers.AppendAllText(_outputAllFileName!, json, Encoding.UTF8);
        }

        private void OutputAllTsvFile()
        {
            var columns = GetOutputAllColumns();
            EnsureOutputAllTsvFileHeader(_outputAllFileName!, columns);
            OutputAllTsvFileRow(_outputAllFileName!, columns);
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
            var file = _values.GetOrDefault("output.each.file.name", "each.{run.time}." + GetOutputEachFileType())!;

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
            return _values.GetOrDefault("output.each.file.type", "tsv")!;
        }

        private string[] GetOutputEachColumns()
        {
            bool hasColumns = _values.Contains("output.each.tsv.file.columns");
            if (hasColumns) return _values.Get("output.each.tsv.file.columns")!.Split(';', '\r', '\n', '\t');

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

        private void OutputSessionStarted(SessionEventArgs e)
        {
            if (!_outputAll && !_outputEach) return;

            var id = e.SessionId;
            var timestamp = this.CreateTimestamp();
            EnsureCacheAll("recognizer.session.started.sessionid", id);
            EnsureOutputEach("recognizer.session.started.sessionid", id);
            EnsureCacheAll("recognizer.session.event.sessionid", id);
            EnsureOutputEach("recognizer.session.event.sessionid", id);
            EnsureCacheAll("recognizer.event.sessionid", id);
            EnsureOutputEach("recognizer.event.sessionid", id);
            EnsureCacheAll("event.sessionid", id);
            EnsureOutputEach("event.sessionid", id);

            EnsureCacheAll("recognizer.session.started.timestamp", timestamp);
            EnsureOutputEach("recognizer.session.started.timestamp", timestamp);
            EnsureCacheAll("recognizer.session.event.timestamp", timestamp);
            EnsureOutputEach("recognizer.session.event.timestamp", timestamp);
            EnsureCacheAll("recognizer.event.timestamp", timestamp);
            EnsureOutputEach("recognizer.event.timestamp", timestamp);
            EnsureCacheAll("event.timestamp", timestamp);
            EnsureOutputEach("event.timestamp", timestamp);

            EnsureOutputRecognizerProperties(
                "recognizer.session.started.recognizer",
                "recognizer.session.event.recognizer");

            var output = $"SESSION STARTED";
            EnsureCacheAll("recognizer.session.started.events", output);
            EnsureOutputEach("recognizer.session.started.event", output);
            EnsureCacheAll("recognizer.session.events", output);
            EnsureOutputEach("recognizer.session.event", output);
            EnsureCacheAll("recognizer.events", output);
            EnsureOutputEach("recognizer.event", output);
            EnsureCacheAll("events", output);
            EnsureOutputEach("event", output);

            FlushOutputEachCacheStage1();
        }

        private void OutputSessionStopped(SessionEventArgs e)
        {
            if (!_outputAll && !_outputEach) return;

            var id = e.SessionId;
            var timestamp = this.CreateTimestamp();
            EnsureCacheAll("recognizer.session.stopped.sessionid", id);
            EnsureOutputEach("recognizer.session.stopped.sessionid", id);
            EnsureCacheAll("recognizer.session.event.sessionid", id);
            EnsureOutputEach("recognizer.session.event.sessionid", id);
            EnsureCacheAll("recognizer.event.sessionid", id);
            EnsureOutputEach("recognizer.event.sessionid", id);
            EnsureCacheAll("event.sessionid", id);
            EnsureOutputEach("event.sessionid", id);

            EnsureCacheAll("recognizer.session.stopped.timestamp", timestamp);
            EnsureOutputEach("recognizer.session.stopped.timestamp", timestamp);
            EnsureCacheAll("recognizer.session.event.timestamp", timestamp);
            EnsureOutputEach("recognizer.session.event.timestamp", timestamp);
            EnsureCacheAll("recognizer.event.timestamp", timestamp);
            EnsureOutputEach("recognizer.event.timestamp", timestamp);
            EnsureCacheAll("event.timestamp", timestamp);
            EnsureOutputEach("event.timestamp", timestamp);

            EnsureOutputRecognizerProperties(
                "recognizer.session.stopped.recognizer",
                "recognizer.session.event.recognizer");

            var output = $"SESSION STOPPED";
            EnsureCacheAll("recognizer.session.stopped.events", output);
            EnsureOutputEach("recognizer.session.stopped.event", output);
            EnsureCacheAll("recognizer.session.events", output);
            EnsureOutputEach("recognizer.session.event", output);
            EnsureCacheAll("recognizer.events", output);
            EnsureOutputEach("recognizer.event", output);
            EnsureCacheAll("events", output);
            EnsureOutputEach("event", output);

            FlushOutputEachCacheStage1();
        }

        private void OutputConnected(ConnectionEventArgs e)
        {
            if (!_outputAll && !_outputEach) return;

            var id = e.SessionId;
            var timestamp = this.CreateTimestamp();
            EnsureCacheAll("connection.connected.sessionid", id);
            EnsureOutputEach("connection.connected.sessionid", id);
            EnsureCacheAll("connection.event.sessionid", id);
            EnsureOutputEach("connection.event.sessionid", id);
            EnsureCacheAll("event.sessionid", id);
            EnsureOutputEach("event.sessionid", id);

            EnsureCacheAll("connection.connected.timestamp", timestamp);
            EnsureOutputEach("connection.connected.timestamp", timestamp);
            EnsureCacheAll("connection.event.timestamp", timestamp);
            EnsureOutputEach("connection.event.timestamp", timestamp);
            EnsureCacheAll("event.timestamp", timestamp);
            EnsureOutputEach("event.timestamp", timestamp);

            var output = $"CONNECTED";
            EnsureCacheAll("connection.connected.events", output);
            EnsureOutputEach("connection.connected.event", output);
            EnsureCacheAll("connection.events", output);
            EnsureOutputEach("connection.event", output);
            EnsureCacheAll("events", output);
            EnsureOutputEach("event", output);

            FlushOutputEachCacheStage1();
        }

        public bool NeedsItnText()
        {
            return GetOutputAllColumns().Count(x => x.Contains(".itn.text")) > 0 ||
                   GetOutputEachColumns().Count(x => x.Contains(".itn.text")) > 0 ||
                   _values.Names.Count(x => x.Contains(".itn.text")) > 0 ||
                   _outputBatch;
        }

        public bool NeedsLexicalText()
        {
            return GetOutputAllColumns().Count(x => x.Contains(".lexical.text")) > 0 ||
                   GetOutputEachColumns().Count(x => x.Contains(".lexical.text")) > 0 ||
                   _values.Names.Count(x => x.Contains(".lexical.text")) > 0 ||
                   _outputBatch;
        }

        private void OutputDisconnected(ConnectionEventArgs e)
        {
            if (!_outputAll && !_outputEach) return;

            var id = e.SessionId;
            var timestamp = this.CreateTimestamp();
            EnsureCacheAll("connection.disconnected.sessionid", id);
            EnsureOutputEach("connection.disconnected.sessionid", id);
            EnsureCacheAll("connection.event.sessionid", id);
            EnsureOutputEach("connection.event.sessionid", id);
            EnsureCacheAll("event.sessionid", id);
            EnsureOutputEach("event.sessionid", id);

            EnsureCacheAll("connection.disconnected.timestamp", timestamp);
            EnsureOutputEach("connection.disconnected.timestamp", timestamp);
            EnsureCacheAll("connection.event.timestamp", timestamp);
            EnsureOutputEach("connection.event.timestamp", timestamp);
            EnsureCacheAll("event.timestamp", timestamp);
            EnsureOutputEach("event.timestamp", timestamp);

            var output = $"DISCONNECTED";
            EnsureCacheAll("connection.disconnected.events", output);
            EnsureOutputEach("connection.disconnected.event", output);
            EnsureCacheAll("connection.events", output);
            EnsureOutputEach("connection.event", output);
            EnsureCacheAll("events", output);
            EnsureOutputEach("event", output);

            FlushOutputEachCacheStage1();
        }

        private void OutputConnectionMessageReceived(ConnectionMessageEventArgs e)
        {
            if (!_outputAll && !_outputEach) return;

            var output = $"MESSAGE RECEIVED";
            EnsureCacheAll("connection.message.received.events", output);
            EnsureOutputEach("connection.message.received.event", output);
            EnsureCacheAll("connection.events", output);
            EnsureOutputEach("connection.event", output);
            if (_verbose) EnsureCacheAll("events", output);
            if (_verbose) EnsureOutputEach("event", output);

            var message = e.Message;
            OutputConnectionMessage("connection.message.received", message, '\n');

            FlushOutputEachCacheStage1();
        }

       private void OutputConnectionMessage(string namePrefix, ConnectionMessage message, char textSeparator)
        {
            if (!_outputAll && !_outputEach) return;

            var path = message.Path;
            EnsureCacheAll(namePrefix + ".path", path);
            EnsureOutputEach(namePrefix + ".path", path);

            var requestId = message.Properties.GetProperty("X-RequestId");
            EnsureCacheAll(namePrefix + ".request.id", requestId);
            EnsureOutputEach(namePrefix + ".request.id", requestId);

            var contentType = message.Properties.GetProperty("Content-Type");
            EnsureCacheAll(namePrefix + ".content.type", contentType);
            EnsureOutputEach(namePrefix + ".content.type", contentType);

            var isBinaryMessage = message.IsBinaryMessage() ? "true" : "false";
            EnsureCacheAll(namePrefix + ".is.binary.message", isBinaryMessage);
            EnsureOutputEach(namePrefix + ".is.binary.message", isBinaryMessage);

            if (message.IsBinaryMessage())
            {
                var binaryMessage = message.GetBinaryMessage();

                var size = binaryMessage.Length.ToString();
                EnsureCacheAll(namePrefix + ".binary.message.size", size);
                EnsureOutputEach(namePrefix + ".binary.message.size", size);

                var hex = string.Concat(binaryMessage.Select(x => x.ToString("X2") + " "));
                EnsureCacheAll(namePrefix + ".binary.message", hex);
                EnsureOutputEach(namePrefix + ".binary.message", hex);
            }

            var isTextMessage = message.IsTextMessage() ? "true" : "false";
            EnsureCacheAll(namePrefix + ".is.text.message", isTextMessage);
            EnsureOutputEach(namePrefix + ".is.text.message", isTextMessage);

            if (message.IsTextMessage())
            {
                var textMessage = message.GetTextMessage();
                EnsureCacheAll(namePrefix + ".text.message", textMessage);
                EnsureOutputEach(namePrefix + ".text.message", textMessage);
            }

            EnsureOutputAllProperties(namePrefix, message.Properties);
            EnsureOutputEachProperty(namePrefix, message.Properties);
        }
 
        private void OutputRecognizing(SpeechRecognitionEventArgs e)
        {
            EnsureCacheOutputResult(e.Result);

            if (!_outputAll && !_outputEach) return;

            var sessionid = e.SessionId;
            var timestamp = this.CreateTimestamp();
            EnsureCacheAll("recognizer.recognizing.sessionid", sessionid);
            EnsureOutputEach("recognizer.recognizing.sessionid", sessionid);
            EnsureCacheAll("recognizer.event.sessionid", sessionid);
            EnsureOutputEach("recognizer.event.sessionid", sessionid);
            EnsureCacheAll("event.sessionid", sessionid);
            EnsureOutputEach("event.sessionid", sessionid);

            EnsureCacheAll("recognizer.recognizing.timestamp", timestamp);
            EnsureOutputEach("recognizer.recognizing.timestamp", timestamp);
            EnsureCacheAll("recognizer.event.timestamp", timestamp);
            EnsureOutputEach("recognizer.event.timestamp", timestamp);
            EnsureCacheAll("event.timestamp", timestamp);
            EnsureOutputEach("event.timestamp", timestamp);

            EnsureOutputRecognizerProperties("recognizer.recognizing.recognizer");

            var result = e.Result;
            OutputResult("recognizer.recognizing.result", result, '\n');
            OutputResult("result", result, '\n'); // Don't include this in global requests for results

            var output = $"RECOGNIZING";
            EnsureCacheAll("recognizer.recognizing.events", output);
            EnsureOutputEach("recognizer.recognizing.event", output);
            EnsureCacheAll("recognizer.events", output);
            EnsureOutputEach("recognizer.event", output);
            EnsureCacheAll("events", output);
            EnsureOutputEach("event", output);

            FlushOutputEachCacheStage1();
        }

        private void OutputRecognized(SpeechRecognitionEventArgs e)
        {
            EnsureCacheOutputResult(e.Result);

            if (!_outputAll && !_outputEach) return;

            var sessionid = e.SessionId;
            var timestamp = this.CreateTimestamp();
            EnsureCacheAll("recognizer.recognized.sessionid", sessionid);
            EnsureOutputEach("recognizer.recognized.sessionid", sessionid);
            EnsureCacheAll("recognizer.event.sessionid", sessionid);
            EnsureOutputEach("recognizer.event.sessionid", sessionid);
            EnsureCacheAll("event.sessionid", sessionid);
            EnsureOutputEach("event.sessionid", sessionid);

            EnsureCacheAll("recognizer.recognized.timestamp", timestamp);
            EnsureOutputEach("recognizer.recognized.timestamp", timestamp);
            EnsureCacheAll("recognizer.event.timestamp", timestamp);
            EnsureOutputEach("recognizer.event.timestamp", timestamp);
            EnsureCacheAll("event.timestamp", timestamp);
            EnsureOutputEach("event.timestamp", timestamp);

            EnsureOutputRecognizerProperties("recognizer.recognized.recognizer");

            var result = e.Result;
            OutputResult("recognizer.recognized.result", result, ' ');
            OutputResult("result", result, ' ');

            var output = $"RECOGNIZED";
            EnsureCacheAll("recognizer.recognized.events", output);
            EnsureOutputEach("recognizer.recognized.event", output);
            EnsureCacheAll("recognizer.events", output);
            EnsureOutputEach("recognizer.event", output);
            EnsureCacheAll("events", output);
            EnsureOutputEach("event", output);

            FlushOutputEachCacheStage1();
        }

        private void OutputCanceled(SpeechRecognitionCanceledEventArgs e)
        {
            EnsureCacheOutputResult(e.Result);

            if (!_outputAll && !_outputEach) return;

            var sessionid = e.SessionId;
            var timestamp = this.CreateTimestamp();
            EnsureCacheAll("recognizer.canceled.sessionid", sessionid);
            EnsureOutputEach("recognizer.canceled.sessionid", sessionid);
            EnsureCacheAll("recognizer.event.sessionid", sessionid);
            EnsureOutputEach("recognizer.event.sessionid", sessionid);
            EnsureCacheAll("event.sessionid", sessionid);
            EnsureOutputEach("event.sessionid", sessionid);

            EnsureCacheAll("recognizer.canceled.timestamp", timestamp);
            EnsureOutputEach("recognizer.canceled.timestamp", timestamp);
            EnsureCacheAll("recognizer.event.timestamp", timestamp);
            EnsureOutputEach("recognizer.event.timestamp", timestamp);
            EnsureCacheAll("event.timestamp", timestamp);
            EnsureOutputEach("event.timestamp", timestamp);

            var reason = e.Reason.ToString();
            EnsureCacheAll("recognizer.canceled.reason", reason);
            EnsureOutputEach("recognizer.canceled.reason", reason);

            var code = (e.Reason == CancellationReason.Error) ? e.ErrorCode.ToString() : "0";
            EnsureCacheAll("recognizer.canceled.error.code", code);
            EnsureOutputEach("recognizer.canceled.error.code", code);
            EnsureCacheAll("recognizer.canceled.error", code);
            EnsureOutputEach("recognizer.canceled.error", code);

            var details = (e.Reason == CancellationReason.Error) ? e.ErrorDetails : "";
            EnsureCacheAll("recognizer.canceled.error.details", details);
            EnsureOutputEach("recognizer.canceled.error.details", details);
            EnsureCacheAll("recognizer.canceled.error", details);
            EnsureOutputEach("recognizer.canceled.error", details);

            EnsureOutputRecognizerProperties("recognizer.canceled.recognizer");

            var result = e.Result;
            OutputResult("recognizer.canceled.result", result, ' ');
            OutputResult("result", result, ' ');

            var output = $"CANCELED";
            EnsureCacheAll("recognizer.session.events", output);
            EnsureOutputEach("recognizer.session.event", output);
            EnsureCacheAll("recognizer.canceled.events", output);
            EnsureOutputEach("recognizer.canceled.event", output);
            EnsureCacheAll("recognizer.events", output);
            EnsureOutputEach("recognizer.event", output);
            EnsureCacheAll("events", output);
            EnsureOutputEach("event", output);

            FlushOutputEachCacheStage1();
        }

        private void OutputResult(string namePrefix, SpeechRecognitionResult result, char textSeparator)
        {
            var id = result.ResultId;
            EnsureCacheAll(namePrefix + ".resultid", id);
            EnsureOutputEach(namePrefix + ".resultid", id);

            var reason = result.Reason.ToString();
            EnsureCacheAll(namePrefix + ".reason", reason);
            EnsureOutputEach(namePrefix + ".reason", reason);

            var offset = result.OffsetInTicks.ToString();
            EnsureCacheAll(namePrefix + ".offset", offset);
            EnsureOutputEach(namePrefix + ".offset", offset);

            var duration = (result.Duration.TotalMilliseconds * 10000).ToString();
            EnsureCacheAll(namePrefix + ".duration", duration);
            EnsureOutputEach(namePrefix + ".duration", duration);

            var text = result.Text;
            EnsureCacheAll(textSeparator, namePrefix + ".text", text);
            EnsureOutputEach(namePrefix + ".text", text);

            var itn = result.Properties.GetProperty("ITN");
            EnsureCacheAll(textSeparator, namePrefix + ".itn.text", itn);
            EnsureOutputEach(namePrefix + ".itn.text", itn);

            var lexical = result.Properties.GetProperty("Lexical");
            EnsureCacheAll(textSeparator, namePrefix + ".lexical.text", lexical);
            EnsureOutputEach(namePrefix + ".lexical.text", lexical);

            var latency = result.Properties.GetProperty(PropertyId.SpeechServiceResponse_RecognitionLatencyMs);
            EnsureCacheAll(namePrefix + ".latency", latency);
            EnsureOutputEach(namePrefix + ".latency", latency);

            var json = result.Properties.GetProperty(PropertyId.SpeechServiceResponse_JsonResult);
            EnsureCacheAll(namePrefix + ".json", json);
            EnsureOutputEach(namePrefix + ".json", json);

            EnsureOutputAllProperties(namePrefix, result.Properties);
            EnsureOutputEachProperty(namePrefix, result.Properties);
        }

        private void EnsureOutputAllProperties(string namePrefix, PropertyCollection properties)
        {
            var outputAll = StringHelpers.WhereLeftRightTrim(_values.Names, $"output.all.{namePrefix}.", ".property");
            foreach (var x in outputAll.Where(x => _values.GetOrDefault($"output.all.{namePrefix}.{x}.property", false)))
            {
                var idOk = Enum.TryParse<PropertyId>(x, true, out PropertyId propid);
                var value = idOk ? properties.GetProperty(propid, "") : properties.GetProperty(x, "");

                var valueOk = !string.IsNullOrEmpty(value);
                if (valueOk) EnsureCacheAll($"{namePrefix}.{x}.property", value);
            }
        }

        private void EnsureOutputEachProperty(string namePrefix, PropertyCollection properties)
        {
            var outputEach = StringHelpers.WhereLeftRightTrim(_values.Names, $"output.each.{namePrefix}.", ".property");
            foreach (var x in outputEach.Where(x => _values.GetOrDefault($"output.each.{namePrefix}.{x}.property", false)))
            {
                var idOk = Enum.TryParse<PropertyId>(x, true, out PropertyId propid);
                var value = idOk ? properties.GetProperty(propid, "") : properties.GetProperty(x, "");
                
                var valueOk = !string.IsNullOrEmpty(value);
                if (valueOk) EnsureOutputEach($"{namePrefix}.{x}.property", value);
            }
        }

        private void EnsureOutputRecognizerProperties(params string[] namePrefixes)
        {
            var properties = GetPropertyCollection("recognizer");
            if (properties != null)
            {
                EnsureOutputProperties(properties, namePrefixes);
                EnsureOutputProperties(properties,
                    "recognizer.event.recognizer",
                    "event.recognizer",
                    "recognizer");
            }
        }

        private void EnsureOutputProperties(PropertyCollection properties, params string[] namePrefixes)
        {
            foreach (var namePrefix in namePrefixes)
            {
                EnsureOutputAllProperties(namePrefix, properties);
                EnsureOutputEachProperty(namePrefix, properties);
            }
        }

        private void OutputRecognizing(IntentRecognitionEventArgs e)
        {
            EnsureCacheOutputResult(e.Result);

            if (!_outputAll && !_outputEach) return;

            var sessionid = e.SessionId;
            var timestamp = this.CreateTimestamp();
            EnsureCacheAll("recognizer.recognizing.sessionid", sessionid);
            EnsureOutputEach("recognizer.recognizing.sessionid", sessionid);
            EnsureCacheAll("recognizer.event.sessionid", sessionid);
            EnsureOutputEach("recognizer.event.sessionid", sessionid);
            EnsureCacheAll("event.sessionid", sessionid);
            EnsureOutputEach("event.sessionid", sessionid);

            EnsureCacheAll("recognizer.recognizing.timestamp", timestamp);
            EnsureOutputEach("recognizer.recognizing.timestamp", timestamp);
            EnsureCacheAll("recognizer.event.timestamp", timestamp);
            EnsureOutputEach("recognizer.event.timestamp", timestamp);
            EnsureCacheAll("event.timestamp", timestamp);
            EnsureOutputEach("event.timestamp", timestamp);

            EnsureOutputRecognizerProperties("recognizer.recognizing.recognizer");

            var result = e.Result;
            OutputResult("recognizer.recognizing.result", result, '\n');
            OutputResult("result", result, '\n'); // Don't include this in global requests for results

            var output = $"RECOGNIZING";
            EnsureCacheAll("recognizer.recognizing.events", output);
            EnsureOutputEach("recognizer.recognizing.event", output);
            EnsureCacheAll("recognizer.events", output);
            EnsureOutputEach("recognizer.event", output);
            EnsureCacheAll("events", output);
            EnsureOutputEach("event", output);

            FlushOutputEachCacheStage1();
        }

        private void OutputRecognized(IntentRecognitionEventArgs e)
        {            
            EnsureCacheOutputResult(e.Result);

            if (!_outputAll && !_outputEach) return;

            var sessionid = e.SessionId;
            var timestamp = this.CreateTimestamp();
            EnsureCacheAll("recognizer.recognized.sessionid", sessionid);
            EnsureOutputEach("recognizer.recognized.sessionid", sessionid);
            EnsureCacheAll("recognizer.event.sessionid", sessionid);
            EnsureOutputEach("recognizer.event.sessionid", sessionid);
            EnsureCacheAll("event.sessionid", sessionid);
            EnsureOutputEach("event.sessionid", sessionid);

            EnsureCacheAll("recognizer.recognized.timestamp", timestamp);
            EnsureOutputEach("recognizer.recognized.timestamp", timestamp);
            EnsureCacheAll("recognizer.event.timestamp", timestamp);
            EnsureOutputEach("recognizer.event.timestamp", timestamp);
            EnsureCacheAll("event.timestamp", timestamp);
            EnsureOutputEach("event.timestamp", timestamp);

            EnsureOutputRecognizerProperties("recognizer.recognized.recognizer");

            var result = e.Result;
            OutputResult("recognizer.recognized.result", result, ' ');
            OutputResult("result", result, ' ');

            var output = $"RECOGNIZED";
            EnsureCacheAll("recognizer.recognized.events", output);
            EnsureOutputEach("recognizer.recognized.event", output);
            EnsureCacheAll("recognizer.events", output);
            EnsureOutputEach("recognizer.event", output);
            EnsureCacheAll("events", output);
            EnsureOutputEach("event", output);

            FlushOutputEachCacheStage1();
        }

        private void OutputCanceled(IntentRecognitionCanceledEventArgs e)
        {
            EnsureCacheOutputResult(e.Result);

            if (!_outputAll && !_outputEach) return;

            var sessionid = e.SessionId;
            var timestamp = this.CreateTimestamp();
            EnsureCacheAll("recognizer.canceled.sessionid", sessionid);
            EnsureOutputEach("recognizer.canceled.sessionid", sessionid);
            EnsureCacheAll("recognizer.event.sessionid", sessionid);
            EnsureOutputEach("recognizer.event.sessionid", sessionid);
            EnsureCacheAll("event.sessionid", sessionid);
            EnsureOutputEach("event.sessionid", sessionid);

            EnsureCacheAll("recognizer.canceled.timestamp", timestamp);
            EnsureOutputEach("recognizer.canceled.timestamp", timestamp);
            EnsureCacheAll("recognizer.event.timestamp", timestamp);
            EnsureOutputEach("recognizer.event.timestamp", timestamp);
            EnsureCacheAll("event.timestamp", timestamp);
            EnsureOutputEach("event.timestamp", timestamp);

            var reason = e.Reason.ToString();
            EnsureCacheAll("recognizer.canceled.reason", reason);
            EnsureOutputEach("recognizer.canceled.reason", reason);

            var code = (e.Reason == CancellationReason.Error) ? e.ErrorCode.ToString() : "0";
            EnsureCacheAll("recognizer.canceled.error.code", code);
            EnsureOutputEach("recognizer.canceled.error.code", code);
            EnsureCacheAll("recognizer.canceled.error", code);
            EnsureOutputEach("recognizer.canceled.error", code);

            var details = (e.Reason == CancellationReason.Error) ? e.ErrorDetails : "";
            EnsureCacheAll("recognizer.canceled.error.details", details);
            EnsureOutputEach("recognizer.canceled.error.details", details);
            EnsureCacheAll("recognizer.canceled.error", details);
            EnsureOutputEach("recognizer.canceled.error", details);

            EnsureOutputRecognizerProperties("recognizer.canceled.recognizer");

            var result = e.Result;
            OutputResult("recognizer.canceled.result", result, ' ');
            OutputResult("result", result, ' ');

            var output = $"CANCELED";
            EnsureCacheAll("recognizer.session.events", output);
            EnsureOutputEach("recognizer.session.event", output);
            EnsureCacheAll("recognizer.canceled.events", output);
            EnsureOutputEach("recognizer.canceled.event", output);
            EnsureCacheAll("recognizer.events", output);
            EnsureOutputEach("recognizer.event", output);
            EnsureCacheAll("events", output);
            EnsureOutputEach("event", output);

            FlushOutputEachCacheStage1();
        }

        private void OutputResult(string namePrefix, IntentRecognitionResult result, char textSeparator)
        {
            var id = result.ResultId;
            EnsureCacheAll(namePrefix + ".resultid", id);
            EnsureOutputEach(namePrefix + ".resultid", id);

            var reason = result.Reason.ToString();
            EnsureCacheAll(namePrefix + ".reason", reason);
            EnsureOutputEach(namePrefix + ".reason", reason);

            var offset = result.OffsetInTicks.ToString();
            EnsureCacheAll(namePrefix + ".offset", offset);
            EnsureOutputEach(namePrefix + ".offset", offset);

            var duration = (result.Duration.TotalMilliseconds * 10000).ToString();
            EnsureCacheAll(namePrefix + ".duration", duration);
            EnsureOutputEach(namePrefix + ".duration", duration);

            var text = result.Text;
            EnsureCacheAll(textSeparator, namePrefix + ".text", text);
            EnsureOutputEach(namePrefix + ".text", text);

            var intent = result.IntentId;
            EnsureCacheAll(namePrefix + ".intentid", intent);
            EnsureOutputEach(namePrefix + ".intentid", intent);

            var itn = result.Properties.GetProperty("ITN");
            EnsureCacheAll(textSeparator, namePrefix + ".itn.text", itn);
            EnsureOutputEach(namePrefix + ".itn.text", itn);

            var lexical = result.Properties.GetProperty("Lexical");
            EnsureCacheAll(textSeparator, namePrefix + ".lexical.text", lexical);
            EnsureOutputEach(namePrefix + ".lexical.text", lexical);

            var latency = result.Properties.GetProperty(PropertyId.SpeechServiceResponse_RecognitionLatencyMs);
            EnsureCacheAll(namePrefix + ".latency", latency);
            EnsureOutputEach(namePrefix + ".latency", latency);

            var json = result.Properties.GetProperty(PropertyId.SpeechServiceResponse_JsonResult);
            EnsureCacheAll(namePrefix + ".json", json);
            EnsureOutputEach(namePrefix + ".json", json);

            var luisJson = result.Properties.GetProperty(PropertyId.LanguageUnderstandingServiceResponse_JsonResult);
            EnsureCacheAll(namePrefix + ".luis.json", luisJson);
            EnsureOutputEach(namePrefix + ".luis.json", luisJson);

            var entityJson = result.Properties.GetProperty("LanguageUnderstandingSLE_JsonResult");
            EnsureOutputIntentResultEntities(namePrefix, entityJson);

            EnsureOutputAllProperties(namePrefix, result.Properties);
            EnsureOutputEachProperty(namePrefix, result.Properties);
        }

        private void EnsureOutputIntentResultEntities(string namePrefix, string entityJson)
        {
            if (!string.IsNullOrEmpty(entityJson))
            {
                EnsureCacheAll(namePrefix + ".entity.json", entityJson);
                EnsureOutputEach(namePrefix + ".entity.json", entityJson);

                var parsed = JsonDocument.Parse(entityJson);

                var outputAll = StringHelpers.WhereLeftRightTrim(_values.Names, $"output.all.{namePrefix}.", ".entity");
                foreach (var x in outputAll.Where(x => _values.GetOrDefault($"output.all.{namePrefix}.{x}.entity", false)))
                {
                    var value = parsed.GetPropertyStringOrNull(x);
                    var valueOk = value != null;
                    if (valueOk) EnsureCacheAll($"{namePrefix}.{x}.entity", value!);
                }

                var outputEach = StringHelpers.WhereLeftRightTrim(_values.Names, $"output.each.{namePrefix}.", ".entity");
                foreach (var x in outputEach.Where(x => _values.GetOrDefault($"output.each.{namePrefix}.{x}.entity", false)))
                {
                    var value = parsed.GetPropertyStringOrNull(x);
                    var valueOk = value != null;
                    if (valueOk) EnsureOutputEach($"{namePrefix}.{x}.entity", value!);
                }
            }
        }

        private void OutputTranscribing(ConversationTranscriptionEventArgs e)
        {
            if (!_outputAll && !_outputEach) return;

            var sessionid = e.SessionId;
            var timestamp = this.CreateTimestamp();
            EnsureCacheAll("recognizer.recognizing.sessionid", sessionid);
            EnsureOutputEach("recognizer.recognizing.sessionid", sessionid);
            EnsureCacheAll("recognizer.event.sessionid", sessionid);
            EnsureOutputEach("recognizer.event.sessionid", sessionid);
            EnsureCacheAll("event.sessionid", sessionid);
            EnsureOutputEach("event.sessionid", sessionid);

            EnsureCacheAll("recognizer.recognizing.timestamp", timestamp);
            EnsureOutputEach("recognizer.recognizing.timestamp", timestamp);
            EnsureCacheAll("recognizer.event.timestamp", timestamp);
            EnsureOutputEach("recognizer.event.timestamp", timestamp);
            EnsureCacheAll("event.timestamp", timestamp);
            EnsureOutputEach("event.timestamp", timestamp);

            EnsureOutputRecognizerProperties("recognizer.recognizing.recognizer");

            var result = e.Result;
            OutputResult("recognizer.recognizing.result", result, '\n');
            OutputResult("result", result, '\n'); // Don't include this in global requests for results

            var output = $"TRANSCRIBING";
            EnsureCacheAll("recognizer.recognizing.events", output);
            EnsureOutputEach("recognizer.recognizing.event", output);
            EnsureCacheAll("recognizer.events", output);
            EnsureOutputEach("recognizer.event", output);
            EnsureCacheAll("events", output);
            EnsureOutputEach("event", output);

            FlushOutputEachCacheStage1();
        }

        private void OutputTranscribed(ConversationTranscriptionEventArgs e)
        {
            EnsureCacheOutputResult(e.Result);

            if (!_outputAll && !_outputEach) return;

            var sessionid = e.SessionId;
            var timestamp = this.CreateTimestamp();
            EnsureCacheAll("recognizer.recognized.sessionid", sessionid);
            EnsureOutputEach("recognizer.recognized.sessionid", sessionid);
            EnsureCacheAll("recognizer.event.sessionid", sessionid);
            EnsureOutputEach("recognizer.event.sessionid", sessionid);
            EnsureCacheAll("event.sessionid", sessionid);
            EnsureOutputEach("event.sessionid", sessionid);

            EnsureCacheAll("recognizer.recognized.timestamp", timestamp);
            EnsureOutputEach("recognizer.recognized.timestamp", timestamp);
            EnsureCacheAll("recognizer.event.timestamp", timestamp);
            EnsureOutputEach("recognizer.event.timestamp", timestamp);
            EnsureCacheAll("event.timestamp", timestamp);
            EnsureOutputEach("event.timestamp", timestamp);

            EnsureOutputRecognizerProperties("recognizer.recognized.recognizer");

            var result = e.Result;
            OutputResult("recognizer.recognized.result", result, ' ');
            OutputResult("result", result, ' ');

            var output = $"TRANSCRIBED";
            EnsureCacheAll("recognizer.recognized.events", output);
            EnsureOutputEach("recognizer.recognized.event", output);
            EnsureCacheAll("recognizer.events", output);
            EnsureOutputEach("recognizer.event", output);
            EnsureCacheAll("events", output);
            EnsureOutputEach("event", output);

            FlushOutputEachCacheStage1();
        }

        private void OutputCanceled(ConversationTranscriptionCanceledEventArgs e)
        {
            EnsureCacheOutputResult(e.Result);

            if (!_outputAll && !_outputEach) return;

            var sessionid = e.SessionId;
            var timestamp = this.CreateTimestamp();
            EnsureCacheAll("recognizer.canceled.sessionid", sessionid);
            EnsureOutputEach("recognizer.canceled.sessionid", sessionid);
            EnsureCacheAll("recognizer.event.sessionid", sessionid);
            EnsureOutputEach("recognizer.event.sessionid", sessionid);
            EnsureCacheAll("event.sessionid", sessionid);
            EnsureOutputEach("event.sessionid", sessionid);

            EnsureCacheAll("recognizer.canceled.timestamp", timestamp);
            EnsureOutputEach("recognizer.canceled.timestamp", timestamp);
            EnsureCacheAll("recognizer.event.timestamp", timestamp);
            EnsureOutputEach("recognizer.event.timestamp", timestamp);
            EnsureCacheAll("event.timestamp", timestamp);
            EnsureOutputEach("event.timestamp", timestamp);

            var reason = e.Reason.ToString();
            EnsureCacheAll("recognizer.canceled.reason", reason);
            EnsureOutputEach("recognizer.canceled.reason", reason);

            var code = (e.Reason == CancellationReason.Error) ? e.ErrorCode.ToString() : "0";
            EnsureCacheAll("recognizer.canceled.error.code", code);
            EnsureOutputEach("recognizer.canceled.error.code", code);
            EnsureCacheAll("recognizer.canceled.error", code);
            EnsureOutputEach("recognizer.canceled.error", code);

            var details = (e.Reason == CancellationReason.Error) ? e.ErrorDetails : "";
            EnsureCacheAll("recognizer.canceled.error.details", details);
            EnsureOutputEach("recognizer.canceled.error.details", details);
            EnsureCacheAll("recognizer.canceled.error", details);
            EnsureOutputEach("recognizer.canceled.error", details);

            EnsureOutputRecognizerProperties("recognizer.canceled.recognizer");

            var result = e.Result;
            OutputResult("recognizer.canceled.result", result, ' ');
            OutputResult("result", result, ' ');

            var output = $"CANCELED";
            EnsureCacheAll("recognizer.session.events", output);
            EnsureOutputEach("recognizer.session.event", output);
            EnsureCacheAll("recognizer.canceled.events", output);
            EnsureOutputEach("recognizer.canceled.event", output);
            EnsureCacheAll("recognizer.events", output);
            EnsureOutputEach("recognizer.event", output);
            EnsureCacheAll("events", output);
            EnsureOutputEach("event", output);

            FlushOutputEachCacheStage1();
        }

        private void OutputResult(string namePrefix, ConversationTranscriptionResult result, char textSeparator)
        {
            var id = result.ResultId;
            EnsureCacheAll(namePrefix + ".resultid", id);
            EnsureOutputEach(namePrefix + ".resultid", id);

            var reason = result.Reason.ToString();
            EnsureCacheAll(namePrefix + ".reason", reason);
            EnsureOutputEach(namePrefix + ".reason", reason);

            var offset = result.OffsetInTicks.ToString();
            EnsureCacheAll(namePrefix + ".offset", offset);
            EnsureOutputEach(namePrefix + ".offset", offset);

            var duration = (result.Duration.TotalMilliseconds * 10000).ToString();
            EnsureCacheAll(namePrefix + ".duration", duration);
            EnsureOutputEach(namePrefix + ".duration", duration);

            var text = result.Text;
            EnsureCacheAll(textSeparator, namePrefix + ".text", text);
            EnsureOutputEach(namePrefix + ".text", text);

            var itn = result.Properties.GetProperty("ITN");
            EnsureCacheAll(textSeparator, namePrefix + ".itn.text", itn);
            EnsureOutputEach(namePrefix + ".itn.text", itn);

            var lexical = result.Properties.GetProperty("Lexical");
            EnsureCacheAll(textSeparator, namePrefix + ".lexical.text", lexical);
            EnsureOutputEach(namePrefix + ".lexical.text", lexical);

            var latency = result.Properties.GetProperty(PropertyId.SpeechServiceResponse_RecognitionLatencyMs);
            EnsureCacheAll(namePrefix + ".latency", latency);
            EnsureOutputEach(namePrefix + ".latency", latency);

            var json = result.Properties.GetProperty(PropertyId.SpeechServiceResponse_JsonResult);
            EnsureCacheAll(namePrefix + ".json", json);
            EnsureOutputEach(namePrefix + ".json", json);

            EnsureOutputAllProperties(namePrefix, result.Properties);
            EnsureOutputEachProperty(namePrefix, result.Properties);
        }

        private void OutputTranscribing(MeetingTranscriptionEventArgs e)
        {
            if (!_outputAll && !_outputEach) return;

            var sessionid = e.SessionId;
            var timestamp = this.CreateTimestamp();
            EnsureCacheAll("recognizer.recognizing.sessionid", sessionid);
            EnsureOutputEach("recognizer.recognizing.sessionid", sessionid);
            EnsureCacheAll("recognizer.event.sessionid", sessionid);
            EnsureOutputEach("recognizer.event.sessionid", sessionid);
            EnsureCacheAll("event.sessionid", sessionid);
            EnsureOutputEach("event.sessionid", sessionid);

            EnsureCacheAll("recognizer.recognizing.timestamp", timestamp);
            EnsureOutputEach("recognizer.recognizing.timestamp", timestamp);
            EnsureCacheAll("recognizer.event.timestamp", timestamp);
            EnsureOutputEach("recognizer.event.timestamp", timestamp);
            EnsureCacheAll("event.timestamp", timestamp);
            EnsureOutputEach("event.timestamp", timestamp);

            EnsureOutputRecognizerProperties("recognizer.recognizing.recognizer");

            var result = e.Result;
            OutputResult("recognizer.recognizing.result", result, '\n');
            OutputResult("result", result, '\n'); // Don't include this in global requests for results

            var output = $"TRANSCRIBING";
            EnsureCacheAll("recognizer.recognizing.events", output);
            EnsureOutputEach("recognizer.recognizing.event", output);
            EnsureCacheAll("recognizer.events", output);
            EnsureOutputEach("recognizer.event", output);
            EnsureCacheAll("events", output);
            EnsureOutputEach("event", output);

            FlushOutputEachCacheStage1();
        }

        private void OutputTranscribed(MeetingTranscriptionEventArgs e)
        {
            EnsureCacheOutputResult(e.Result);

            if (!_outputAll && !_outputEach) return;

            var sessionid = e.SessionId;
            var timestamp = this.CreateTimestamp();
            EnsureCacheAll("recognizer.recognized.sessionid", sessionid);
            EnsureOutputEach("recognizer.recognized.sessionid", sessionid);
            EnsureCacheAll("recognizer.event.sessionid", sessionid);
            EnsureOutputEach("recognizer.event.sessionid", sessionid);
            EnsureCacheAll("event.sessionid", sessionid);
            EnsureOutputEach("event.sessionid", sessionid);

            EnsureCacheAll("recognizer.recognized.timestamp", timestamp);
            EnsureOutputEach("recognizer.recognized.timestamp", timestamp);
            EnsureCacheAll("recognizer.event.timestamp", timestamp);
            EnsureOutputEach("recognizer.event.timestamp", timestamp);
            EnsureCacheAll("event.timestamp", timestamp);
            EnsureOutputEach("event.timestamp", timestamp);

            EnsureOutputRecognizerProperties("recognizer.recognized.recognizer");

            var result = e.Result;
            OutputResult("recognizer.recognized.result", result, ' ');
            OutputResult("result", result, ' ');

            var output = $"TRANSCRIBED";
            EnsureCacheAll("recognizer.recognized.events", output);
            EnsureOutputEach("recognizer.recognized.event", output);
            EnsureCacheAll("recognizer.events", output);
            EnsureOutputEach("recognizer.event", output);
            EnsureCacheAll("events", output);
            EnsureOutputEach("event", output);

            FlushOutputEachCacheStage1();
        }

        private void OutputCanceled(MeetingTranscriptionCanceledEventArgs e)
        {
            EnsureCacheOutputResult(e.Result);

            if (!_outputAll && !_outputEach) return;

            var sessionid = e.SessionId;
            var timestamp = this.CreateTimestamp();
            EnsureCacheAll("recognizer.canceled.sessionid", sessionid);
            EnsureOutputEach("recognizer.canceled.sessionid", sessionid);
            EnsureCacheAll("recognizer.event.sessionid", sessionid);
            EnsureOutputEach("recognizer.event.sessionid", sessionid);
            EnsureCacheAll("event.sessionid", sessionid);
            EnsureOutputEach("event.sessionid", sessionid);

            EnsureCacheAll("recognizer.canceled.timestamp", timestamp);
            EnsureOutputEach("recognizer.canceled.timestamp", timestamp);
            EnsureCacheAll("recognizer.event.timestamp", timestamp);
            EnsureOutputEach("recognizer.event.timestamp", timestamp);
            EnsureCacheAll("event.timestamp", timestamp);
            EnsureOutputEach("event.timestamp", timestamp);

            var reason = e.Reason.ToString();
            EnsureCacheAll("recognizer.canceled.reason", reason);
            EnsureOutputEach("recognizer.canceled.reason", reason);

            var code = (e.Reason == CancellationReason.Error) ? e.ErrorCode.ToString() : "0";
            EnsureCacheAll("recognizer.canceled.error.code", code);
            EnsureOutputEach("recognizer.canceled.error.code", code);
            EnsureCacheAll("recognizer.canceled.error", code);
            EnsureOutputEach("recognizer.canceled.error", code);

            var details = (e.Reason == CancellationReason.Error) ? e.ErrorDetails : "";
            EnsureCacheAll("recognizer.canceled.error.details", details);
            EnsureOutputEach("recognizer.canceled.error.details", details);
            EnsureCacheAll("recognizer.canceled.error", details);
            EnsureOutputEach("recognizer.canceled.error", details);

            EnsureOutputRecognizerProperties("recognizer.canceled.recognizer");

            var result = e.Result;
            OutputResult("recognizer.canceled.result", result, ' ');
            OutputResult("result", result, ' ');

            var output = $"CANCELED";
            EnsureCacheAll("recognizer.session.events", output);
            EnsureOutputEach("recognizer.session.event", output);
            EnsureCacheAll("recognizer.canceled.events", output);
            EnsureOutputEach("recognizer.canceled.event", output);
            EnsureCacheAll("recognizer.events", output);
            EnsureOutputEach("recognizer.event", output);
            EnsureCacheAll("events", output);
            EnsureOutputEach("event", output);

            FlushOutputEachCacheStage1();
        }

        private void OutputResult(string namePrefix, MeetingTranscriptionResult result, char textSeparator)
        {
            var id = result.ResultId;
            EnsureCacheAll(namePrefix + ".resultid", id);
            EnsureOutputEach(namePrefix + ".resultid", id);

            var reason = result.Reason.ToString();
            EnsureCacheAll(namePrefix + ".reason", reason);
            EnsureOutputEach(namePrefix + ".reason", reason);

            var offset = result.OffsetInTicks.ToString();
            EnsureCacheAll(namePrefix + ".offset", offset);
            EnsureOutputEach(namePrefix + ".offset", offset);

            var duration = (result.Duration.TotalMilliseconds * 10000).ToString();
            EnsureCacheAll(namePrefix + ".duration", duration);
            EnsureOutputEach(namePrefix + ".duration", duration);

            var text = result.Text;
            EnsureCacheAll(textSeparator, namePrefix + ".text", text);
            EnsureOutputEach(namePrefix + ".text", text);

            var itn = result.Properties.GetProperty("ITN");
            EnsureCacheAll(textSeparator, namePrefix + ".itn.text", itn);
            EnsureOutputEach(namePrefix + ".itn.text", itn);

            var lexical = result.Properties.GetProperty("Lexical");
            EnsureCacheAll(textSeparator, namePrefix + ".lexical.text", lexical);
            EnsureOutputEach(namePrefix + ".lexical.text", lexical);

            var latency = result.Properties.GetProperty(PropertyId.SpeechServiceResponse_RecognitionLatencyMs);
            EnsureCacheAll(namePrefix + ".latency", latency);
            EnsureOutputEach(namePrefix + ".latency", latency);

            var json = result.Properties.GetProperty(PropertyId.SpeechServiceResponse_JsonResult);
            EnsureCacheAll(namePrefix + ".json", json);
            EnsureOutputEach(namePrefix + ".json", json);

            EnsureOutputAllProperties(namePrefix, result.Properties);
            EnsureOutputEachProperty(namePrefix, result.Properties);
        }

        private void OutputRecognizing(TranslationRecognitionEventArgs e)
        {
            EnsureCacheOutputResult(e.Result);

            if (!_outputAll && !_outputEach) return;

            var sessionid = e.SessionId; 
            var timestamp = this.CreateTimestamp();
            EnsureCacheAll("recognizer.recognizing.sessionid", sessionid);
            EnsureOutputEach("recognizer.recognizing.sessionid", sessionid);
            EnsureCacheAll("recognizer.event.sessionid", sessionid);
            EnsureOutputEach("recognizer.event.sessionid", sessionid);
            EnsureCacheAll("event.sessionid", sessionid);
            EnsureOutputEach("event.sessionid", sessionid);

            EnsureCacheAll("recognizer.recognizing.timestamp", timestamp);
            EnsureOutputEach("recognizer.recognizing.timestamp", timestamp);
            EnsureCacheAll("recognizer.event.timestamp", timestamp);
            EnsureOutputEach("recognizer.event.timestamp", timestamp);
            EnsureCacheAll("event.timestamp", timestamp);
            EnsureOutputEach("event.timestamp", timestamp);

            EnsureOutputRecognizerProperties("recognizer.recognizing.recognizer");

            var result = e.Result;
            OutputResult("recognizer.recognizing.result", result, '\n');
            OutputResult("result", result, '\n'); // Don't include this in global requests for results

            var output = $"RECOGNIZING";
            EnsureCacheAll("recognizer.recognizing.events", output);
            EnsureOutputEach("recognizer.recognizing.event", output);
            EnsureCacheAll("recognizer.events", output);
            EnsureOutputEach("recognizer.event", output);
            EnsureCacheAll("events", output);
            EnsureOutputEach("event", output);

            FlushOutputEachCacheStage1();
        }

        private void OutputRecognized(TranslationRecognitionEventArgs e)
        {
            EnsureCacheOutputResult(e.Result);

            if (!_outputAll && !_outputEach) return;

            var sessionid = e.SessionId;
            var timestamp = this.CreateTimestamp();
            EnsureCacheAll("recognizer.recognized.sessionid", sessionid);
            EnsureOutputEach("recognizer.recognized.sessionid", sessionid);
            EnsureCacheAll("recognizer.event.sessionid", sessionid);
            EnsureOutputEach("recognizer.event.sessionid", sessionid);
            EnsureCacheAll("event.sessionid", sessionid);
            EnsureOutputEach("event.sessionid", sessionid);

            EnsureCacheAll("recognizer.recognized.timestamp", timestamp);
            EnsureOutputEach("recognizer.recognized.timestamp", timestamp);
            EnsureCacheAll("recognizer.event.timestamp", timestamp);
            EnsureOutputEach("recognizer.event.timestamp", timestamp); ;
            EnsureCacheAll("event.timestamp", timestamp);
            EnsureOutputEach("event.timestamp", timestamp);

            EnsureOutputRecognizerProperties("recognizer.recognized.recognizer");

            var result = e.Result;
            OutputResult("recognizer.recognized.result", result, ' ');
            OutputResult("result", result, ' ');

            var output = $"RECOGNIZED";
            EnsureCacheAll("recognizer.recognized.events", output);
            EnsureOutputEach("recognizer.recognized.event", output);
            EnsureCacheAll("recognizer.events", output);
            EnsureOutputEach("recognizer.event", output);
            EnsureCacheAll("events", output);
            EnsureOutputEach("event", output);

            FlushOutputEachCacheStage1();
        }

        private void OutputCanceled(TranslationRecognitionCanceledEventArgs e)
        {
            EnsureCacheOutputResult(e.Result);

            if (!_outputAll && !_outputEach) return;

            var sessionid = e.SessionId;
            var timestamp = this.CreateTimestamp();
            EnsureCacheAll("recognizer.canceled.sessionid", sessionid);
            EnsureOutputEach("recognizer.canceled.sessionid", sessionid);
            EnsureCacheAll("recognizer.event.sessionid", sessionid);
            EnsureOutputEach("recognizer.event.sessionid", sessionid);
            EnsureCacheAll("event.sessionid", sessionid);
            EnsureOutputEach("event.sessionid", sessionid);

            EnsureCacheAll("recognizer.canceled.timestamp", timestamp);
            EnsureOutputEach("recognizer.canceled.timestamp", timestamp);
            EnsureCacheAll("recognizer.event.timestamp", timestamp);
            EnsureOutputEach("recognizer.event.timestamp", timestamp);
            EnsureCacheAll("event.timestamp", timestamp);
            EnsureOutputEach("event.timestamp", timestamp);

            var reason = e.Reason.ToString();
            EnsureCacheAll("recognizer.canceled.reason", reason);
            EnsureOutputEach("recognizer.canceled.reason", reason);

            var code = (e.Reason == CancellationReason.Error) ? e.ErrorCode.ToString() : "0";
            EnsureCacheAll("recognizer.canceled.error.code", code);
            EnsureOutputEach("recognizer.canceled.error.code", code);
            EnsureCacheAll("recognizer.canceled.error", code);
            EnsureOutputEach("recognizer.canceled.error", code);

            var details = (e.Reason == CancellationReason.Error) ? e.ErrorDetails : "";
            EnsureCacheAll("recognizer.canceled.error.details", details);
            EnsureOutputEach("recognizer.canceled.error.details", details);
            EnsureCacheAll("recognizer.canceled.error", details);
            EnsureOutputEach("recognizer.canceled.error", details);

            EnsureOutputRecognizerProperties("recognizer.canceled.recognizer");

            var result = e.Result;
            OutputResult("recognizer.canceled.result", result, ' ');
            OutputResult("result", result, ' ');

            var output = $"CANCELED";
            EnsureCacheAll("recognizer.session.events", output);
            EnsureOutputEach("recognizer.session.event", output);
            EnsureCacheAll("recognizer.canceled.events", output);
            EnsureOutputEach("recognizer.canceled.event", output);
            EnsureCacheAll("recognizer.events", output);
            EnsureOutputEach("recognizer.event", output);
            EnsureCacheAll("events", output);
            EnsureOutputEach("event", output);

            FlushOutputEachCacheStage1();
        }

        private void OutputResult(string namePrefix, TranslationRecognitionResult result, char textSeparator)
        {
            var id = result.ResultId;
            EnsureCacheAll(namePrefix + ".resultid", id);
            EnsureOutputEach(namePrefix + ".resultid", id);

            var reason = result.Reason.ToString();
            EnsureCacheAll(namePrefix + ".reason", reason);
            EnsureOutputEach(namePrefix + ".reason", reason);

            var offset = result.OffsetInTicks.ToString();
            EnsureCacheAll(namePrefix + ".offset", offset);
            EnsureOutputEach(namePrefix + ".offset", offset);

            var duration = (result.Duration.TotalMilliseconds * 10000).ToString();
            EnsureCacheAll(namePrefix + ".duration", duration);
            EnsureOutputEach(namePrefix + ".duration", duration);

            var text = result.Text;
            EnsureCacheAll(textSeparator, namePrefix + ".text", text);
            EnsureOutputEach(namePrefix + ".text", text);

            var itn = result.Properties.GetProperty("ITN");
            EnsureCacheAll(textSeparator, namePrefix + ".itn.text", itn);
            EnsureOutputEach(namePrefix + ".itn.text", itn);

            var lexical = result.Properties.GetProperty("Lexical");
            EnsureCacheAll(textSeparator, namePrefix + ".lexical.text", lexical);
            EnsureOutputEach(namePrefix + ".lexical.text", lexical);

            if (result.Translations.Count > 0)
            {
                var translation = result.Translations[result.Translations.Keys.First()];
                EnsureCacheAll(textSeparator, namePrefix + $".translated.text", translation);
                EnsureOutputEach(namePrefix + $".translated.text", translation);
            }

            foreach (var lang in result.Translations.Keys)
            {
                var translation = result.Translations[lang];
                EnsureCacheAll(textSeparator, namePrefix + $".translated.{lang}.text", translation);
                EnsureOutputEach(namePrefix + $".translated.{lang}.text", translation);
            }

            var latency = result.Properties.GetProperty(PropertyId.SpeechServiceResponse_RecognitionLatencyMs);
            EnsureCacheAll(namePrefix + ".latency", latency);
            EnsureOutputEach(namePrefix + ".latency", latency);

            var json = result.Properties.GetProperty(PropertyId.SpeechServiceResponse_JsonResult);
            EnsureCacheAll(namePrefix + ".json", json);
            EnsureOutputEach(namePrefix + ".json", json);

            EnsureOutputAllProperties(namePrefix, result.Properties);
            EnsureOutputEachProperty(namePrefix, result.Properties);
        }

        private void OutputSynthesisStarted(SpeechSynthesisEventArgs e)
        {
            EnsureCacheOutputResult(e.Result);

            if (!_outputAll && !_outputEach) return;

            var output = $"SYNTHESIS STARTED";
            EnsureCacheAll("synthesizer.synthesis.started.events", output);
            EnsureOutputEach("synthesizer.synthesis.started.event", output);
            EnsureCacheAll("synthesizer.events", output);
            EnsureOutputEach("synthesizer.event", output);
            EnsureCacheAll("events", output);
            EnsureOutputEach("event", output);

            var result = e.Result;
            OutputResult("synthesizer.synthesis.started.result", result, '\n');
            OutputResult("result", result, '\n'); // Don't include this in global requests for results

            FlushOutputEachCacheStage1();
        }

        private void OutputSynthesizing(SpeechSynthesisEventArgs e)
        {
            EnsureCacheOutputResult(e.Result);

            if (!_outputAll && !_outputEach) return;

            var output = $"SYNTHESIZING";
            EnsureCacheAll("synthesizer.synthesizing.events", output);
            EnsureOutputEach("synthesizer.synthesizing.event", output);
            EnsureCacheAll("synthesizer.events", output);
            EnsureOutputEach("synthesizer.event", output);
            EnsureCacheAll("events", output);
            EnsureOutputEach("event", output);

            var result = e.Result;
            OutputResult("synthesizer.synthesizing.result", result, '\n');
            OutputResult("result", result, '\n'); // Don't include this in global requests for results

            FlushOutputEachCacheStage1();
        }

        private void OutputSynthesisCompleted(SpeechSynthesisEventArgs e)
        {
            EnsureCacheOutputResult(e.Result);

            if (!_outputAll && !_outputEach) return;

            var output = $"SYNTHESIS COMPLETED";
            EnsureCacheAll("synthesizer.synthesis.completed.events", output);
            EnsureOutputEach("synthesizer.synthesis.completed.event", output);
            EnsureCacheAll("synthesizer.events", output);
            EnsureOutputEach("synthesizer.event", output);
            EnsureCacheAll("events", output);
            EnsureOutputEach("event", output);

            var result = e.Result;
            OutputResult("synthesizer.synthesis.completed.result", result, '\n');
            OutputResult("result", result, '\n'); // Don't include this in global requests for results

            FlushOutputEachCacheStage1();
        }

        private void OutputSynthesisCanceled(SpeechSynthesisEventArgs e)
        {
            EnsureCacheOutputResult(e.Result);

            if (!_outputAll && !_outputEach) return;

            var cancelDetails = SpeechSynthesisCancellationDetails.FromResult(e.Result);

            var reason = cancelDetails.Reason.ToString();
            EnsureCacheAll("synthesizer.synthesis.canceled.reason", reason);
            EnsureOutputEach("synthesizer.synthesis.canceled.reason", reason);

            var code = (cancelDetails.Reason == CancellationReason.Error) ? cancelDetails.ErrorCode.ToString() : "0";
            EnsureCacheAll("synthesizer.synthesis.canceled.error.code", code);
            EnsureOutputEach("synthesizer.synthesis.canceled.error.code", code);
            EnsureCacheAll("synthesizer.synthesis.canceled.error", code);
            EnsureOutputEach("synthesizer.synthesis.canceled.error", code);

            var details = (cancelDetails.Reason == CancellationReason.Error) ? cancelDetails.ErrorDetails : "";
            EnsureCacheAll("synthesizer.synthesis.canceled.error.details", details);
            EnsureOutputEach("synthesizer.synthesis.canceled.error.details", details);
            EnsureCacheAll("synthesizer.synthesis.canceled.error", details);
            EnsureOutputEach("synthesizer.synthesis.canceled.error", details);

            var result = e.Result;
            OutputResult("synthesizer.canceled.result", result, ' ');
            OutputResult("result", result, ' ');

            var output = $"CANCELED";
            EnsureCacheAll("synthesizer.synthesis.canceled.events", output);
            EnsureOutputEach("synthesizer.synthesis.canceled.event", output);
            EnsureCacheAll("synthesizer.events", output);
            EnsureOutputEach("synthesizer.event", output);
            EnsureCacheAll("events", output);
            EnsureOutputEach("event", output);

            FlushOutputEachCacheStage1();
        }

        private void OutputWordBoundary(SpeechSynthesisWordBoundaryEventArgs e)
        {
            if (!_outputAll && !_outputEach) return;

            var output = $"WORD BOUNDARY";
            const string synthesizer = "synthesizer";
            const string wordBoundaryNamePrefix = synthesizer + ".wordboundary";
            EnsureCacheAll(wordBoundaryNamePrefix + ".events", output);
            EnsureOutputEach(wordBoundaryNamePrefix + ".event", output);
            EnsureCacheAll(synthesizer + ".events", output);
            EnsureOutputEach(synthesizer + ".event", output);

            const string wordBoundaryResultNamePrefix = wordBoundaryNamePrefix + ".result";

            //// These are almost always the same value and don't really lend much value. If you find that you
            //// need them for some test scenario, uncomment this
            ////EnsureCacheAll(wordBoundaryResultNamePrefix + ".resultid", e.ResultId);
            ////EnsureOutputEach(wordBoundaryResultNamePrefix + ".resultid", e.ResultId);

            var wordLength = e.WordLength.ToString();
            EnsureCacheAll(wordBoundaryResultNamePrefix + ".wordlength", wordLength);
            EnsureOutputEach(wordBoundaryResultNamePrefix + ".wordlength", wordLength);

            EnsureCacheAll(wordBoundaryResultNamePrefix + ".text", e.Text);
            EnsureOutputEach(wordBoundaryResultNamePrefix + ".text", e.Text);

            var boundaryType = e.BoundaryType.ToString();
            EnsureCacheAll(wordBoundaryResultNamePrefix + ".type", boundaryType);
            EnsureOutputEach(wordBoundaryResultNamePrefix + ".type", boundaryType);

            var boundaryOffset = e.AudioOffset.ToString();
            EnsureCacheAll(wordBoundaryResultNamePrefix + ".offset", boundaryOffset);
            EnsureOutputEach(wordBoundaryResultNamePrefix + ".offset", boundaryOffset);

            var boundaryDuration = e.Duration.ToString();
            EnsureCacheAll(wordBoundaryResultNamePrefix + ".duration", boundaryDuration);
            EnsureOutputEach(wordBoundaryResultNamePrefix + ".duration", boundaryDuration);

            FlushOutputEachCacheStage1();
        }

        private void OutputResult(string namePrefix, SpeechSynthesisResult result, char textSeparator)
        {
            var id = result.ResultId;
            EnsureCacheAll(namePrefix + ".resultid", id);
            EnsureOutputEach(namePrefix + ".resultid", id);

            var reason = result.Reason.ToString();
            EnsureCacheAll(namePrefix + ".reason", reason);
            EnsureOutputEach(namePrefix + ".reason", reason);

            var audio= string.Concat(result.AudioData.Select(x => x.ToString("X2") + " "));
            EnsureCacheAll(namePrefix + ".audio.data", audio);
            EnsureOutputEach(namePrefix + ".audio.data", audio);

            var length = result.AudioData.Length.ToString();
            EnsureCacheAll(namePrefix + ".audio.length", length);
            EnsureOutputEach(namePrefix + ".audio.length", length);

            //var latency = result.Properties.GetProperty(PropertyId.???);
            //EnsureOutputAll(namePrefix + ".latency", latency);
            //EnsureOutputEach(namePrefix + ".latency", latency);
        }

        public void CheckOutput()
        {
            var checkTranscript = _values.Names.Count(x => x.Contains("check.sr.transcript.")) > 0;
            if (checkTranscript) CheckTranscript();

            CheckAll();
            CheckResult();
        }

        private void CheckAll()
        {
            var jmesValue = _values.GetOrDefault("check.jmes", null);
            if (jmesValue != null)
            {
                var json = JsonHelpers.GetJsonObjectText(_checkAllCache!);
                if(String.IsNullOrEmpty(json))
                {
                    SetPassed(false);
                    ColorHelpers.SetErrorColors();
                    Console.WriteLine();
                    Console.WriteLine($"Check command was specified, but there is nothing to check against");
                    Console.WriteLine();
                    ColorHelpers.ResetColor();
                    return;
                }

                var jmes = new JmesPath();
                var jmesResult = jmes.Transform(json, jmesValue);

                // If this is "false" they wrote an expression that returned "false"
                // if this is "null" they wrote an expression looking for an item that doesn't exist.
                if (String.Compare("null", jmesResult, true) == 0 ||
                    String.Compare(Boolean.FalseString, jmesResult, true) == 0)
                {
                    SetPassed(false);

                    ColorHelpers.SetErrorColors();
                    Console.WriteLine();
                    Console.WriteLine($"No match found for given query script:" + Environment.NewLine + jmesValue);
                    Console.WriteLine();
                    var verboseOutput = _values.Contains("check.jmes.verbose.failures");
                    if (verboseOutput) Console.WriteLine("Full output:" + Environment.NewLine + json);
                    ColorHelpers.ResetColor();
                }
            }
        }

        private void CheckResult()
        {
            var jmesValue = _values.GetOrDefault("check.result.jmes", null);
            if (jmesValue != null)
            {
                // default to a failed state to prevent false positives
                bool checkEval = false;

                // Match our detailed result.
                foreach (RecognitionResult result in _outputResultCache!)
                {
                    // TODO: add other result types
                    if (result.Reason == ResultReason.RecognizedSpeech || result.Reason == ResultReason.TranslatedSpeech ||
                        result.Reason == ResultReason.Canceled || result.Reason == ResultReason.NoMatch)
                    {
                        var jmes = new JmesPath();

                        var serviceResultJson = result.Properties.GetProperty(PropertyId.SpeechServiceResponse_JsonResult);
                        if(String.IsNullOrEmpty(serviceResultJson))
                        {
                            checkEval = false;
                            ColorHelpers.SetErrorColors();
                            Console.WriteLine();
                            Console.WriteLine($"Check result command specified, but no result json was returned from the service");
                            Console.WriteLine();
                            ColorHelpers.ResetColor();
                            break;
                        }

                        var jmesResult = jmes.Transform(serviceResultJson, jmesValue);

                        // If this is "false" they wrote an expression that returned "false"
                        // if this is "null" they wrote an expression looking for an item that doesn't exist.
                        if (String.Compare("null", jmesResult, true) == 0 ||
                            String.Compare(Boolean.FalseString, jmesResult, true) == 0)
                        {
                            checkEval = false;
                            ColorHelpers.SetErrorColors();
                            Console.WriteLine();
                            Console.WriteLine($"Check script: {jmesValue}");
                            Console.WriteLine($"JSON: {serviceResultJson}");
                            Console.WriteLine();
                            Console.WriteLine($"Check result: Failed");
                            ColorHelpers.ResetColor();
                        }
                        else
                        {
                            checkEval = true;
                            break; //is this problematic? Should we be checking all entries?
                        }
                    }
                }
                SetPassed(checkEval);
            }
        }

        private void CheckTranscript()
        {
            CheckTranscriptWer();
            CheckTranscriptText();
        }

        private void CheckTranscriptWer()
        {
            CheckTranscriptWer("text");
            CheckTranscriptWer("itn.text");
            CheckTranscriptWer("lexical.text");
            CheckTranslationWer();
        }

        private void CheckTranslationWer()
        {
            CheckTranscriptWer($"translated.text");

            var languages = _values.Names
                .Where(x => x.StartsWith("check.sr.transcript.translated.") && x.EndsWith(".text.wer"))
                .Select(x => x.Replace("check.sr.transcript.translated.", "").Replace(".text.wer", ""));

            foreach (var lang in languages)
            {
                CheckTranscriptWer($"translated.{lang}.text");
            }
        }

        private void CheckTranscriptWer(string check)
        {
            var transcriptName = "transcript." + check;
            var transcript = _values.GetOrDefault(transcriptName, null);
            if (transcript == null) transcript = _values.GetOrDefault("transcript.text", null);
            if (transcript == null) return;

            var textName = "recognizer.recognized.result." + check;
            var text = GetAllOutput(textName)!;
            if (text == null) return;

            var checkName = "check.sr.transcript." + check + ".wer";
            var checking = _values[checkName];
            if (checking == null) return;

            var ignorePunctuation = _values.GetOrDefault("wer.ignore.punctuation", true);
            var werUrl = _values["wer.sr.url"];
            var isUrlOk = !string.IsNullOrEmpty(werUrl) && werUrl.StartsWith("http");
            var wer = isUrlOk ? 
                GetWerFromUrl(werUrl!, transcript, text, _targetCulture!.Name) : 
                WerHelpers.CalculateWer(transcript, text, _targetCulture!, ignorePunctuation);
            var pass = CheckNumber(wer.ErrorRate, checkName, checking);
            if (!pass) SetPassed(false);

            _values.Add("output.all." + transcriptName + ".wer.words", "true");
            EnsureCacheAll(transcriptName + ".wer.words", wer.Words.ToString());

            _values.Add("output.all." + transcriptName + ".wer.errors", "true");
            EnsureCacheAll(transcriptName + ".wer.errors", wer.Errors.ToString());

            _values.Add("output.all." + transcriptName + ".wer", "true");
            EnsureCacheAll(transcriptName + ".wer", wer.ErrorRate.ToString());

            _values.Add("output.all." + transcriptName, "true");
            EnsureCacheAll(transcriptName, transcript);

            _values.Add("output.all." + checkName, "true");
            EnsureCacheAll(checkName, pass ? "true" : "false");

            DebugWriteCheckTranscriptWer(transcriptName, transcript, textName, text, checkName, checking, pass);
        }

        private WerFraction GetWerFromUrl(string werUrl, string transcript, string text, string locale)
        {
            var lang = string.IsNullOrEmpty(locale) ? "" : $"&locale={locale}";
            var transcription = string.IsNullOrEmpty(locale) ? "" : $"&transcription={transcript}";
            var recognizedText = string.IsNullOrEmpty(locale) ? "" : $"&recognition={text}";
            var query = $"{lang}{transcription}{recognizedText}";

            var request = (HttpWebRequest)WebRequest.Create($"{werUrl}{query}");
            var response = request.GetResponse();
            if(!response.ContentType.Contains("application/json"))
            {
                _values.AddThrowError("WARNING:", $"GET to external function did not return valid json payload!\n");
            }

            using (var responseStream = response.GetResponseStream())
            {
                using (var jsonStream = new StreamReader(responseStream))
                {
                    var json = jsonStream.ReadToEnd();
                    var parsed = JsonDocument.Parse(json);
                    var wordCount = parsed.RootElement.GetProperty("wordCount").GetInt32();
                    var errors = parsed.RootElement.GetProperty("errors").GetInt32();
                    return new WerFraction(wordCount, errors);
                }
            }
        }

        private bool CheckNumber(int n1, string checkName, string checking)
        {
            int n2 = 0;
            bool pass =
                (checking.StartsWith("eq=") && int.TryParse(checking.Substring(3), out n2) && n1 == n2) ||
                (checking.StartsWith("ne=") && int.TryParse(checking.Substring(3), out n2) && n1 != n2) ||
                (checking.StartsWith("le=") && int.TryParse(checking.Substring(3), out n2) && n1 <= n2) ||
                (checking.StartsWith("lt=") && int.TryParse(checking.Substring(3), out n2) && n1 < n2) ||
                (checking.StartsWith("gt=") && int.TryParse(checking.Substring(3), out n2) && n1 > n2) ||
                (checking.StartsWith("ge=") && int.TryParse(checking.Substring(3), out n2) && n1 >= n2) ||
                (int.TryParse(checking, out n2) && n1 == n2);

            return pass;
        }

        private void CheckTranscriptText()
        {
            CheckTranscriptText("text");
            CheckTranscriptText("itn.text");
            CheckTranscriptText("lexical.text");
        }

        private void CheckTranscriptText(string check)
        {
            var textName = "recognizer.recognized.result." + check;
            var text = GetAllOutput(textName)!;

            var checkName = "check.sr.transcript." + check;

            const string prefix = "check.sr.transcript.";
            if (_values.Contains(prefix + check)) CheckTranscriptTextExpr(textName, text, check);
            if (_values.Contains(prefix + check + ".in")) CheckTranscriptTextIn(textName, text, check);
            if (_values.Contains(prefix + check + ".contains")) CheckTranscriptTextContains(textName, text, check);
            if (_values.Contains(prefix + check + ".not.in")) CheckTranscriptTextNotIn(textName, text, check);
            if (_values.Contains(prefix + check + ".not.contains")) CheckTranscriptTextNotContains(textName, text, check);
        }

        private void CheckTranscriptTextExpr(string textName, string text, string check)
        {
            var checkName = "check.sr.transcript." + check;
            var checking = _values[checkName] ?? string.Empty;

            if (checking.StartsWith("eq="))
            {
                CheckTranscriptTextEqual(textName, text, check, checkName, checking.Substring(3));
            }
            else if (checking.StartsWith("ne="))
            {
                CheckTranscriptTextNotEqual(textName, text, check, checkName, checking.Substring(3));
            }
            else
            {
                CheckTranscriptTextEqual(textName, text, check, checkName, checking);
            }
        }

        private void CheckTranscriptTextEqual(string textName, string text, string check, string checkName, string checking)
        {
            var pass = checking.Split(';', '\r', '\n').Count(x => x.CompareTo(text) == 0) > 0;
            if (!pass) SetPassed(false);

            _values.Add("output.all." + checkName, "true");
            EnsureCacheAll(checkName, pass ? "true" : "false");

            DebugWriteCheckTranscriptText(textName, text, checkName, checking, pass);
        }

        private void CheckTranscriptTextNotEqual(string textName, string text, string check, string checkName, string checking)
        {
            var pass = checking.Split(';', '\r', '\n').Count(x => x.CompareTo(text) == 0) == 0;
            if (!pass) SetPassed(false);

            _values.Add("output.all." + checkName, "true");
            EnsureCacheAll(checkName, pass ? "true" : "false");

            DebugWriteCheckTranscriptText(textName, text, checkName, checking, pass);
        }

        private void CheckTranscriptTextIn(string textName, string text, string check)
        {
            var checkName = "check.sr.transcript." + check + ".in";
            var checking = _values[checkName];

            var checkItems = checking?.Split(';', '\r', '\n');
            var pass = checkItems?.Count(x => x.CompareTo(text) == 0) > 0;
            if (!pass) SetPassed(false);

            _values.Add("output.all." + checkName, "true");
            EnsureCacheAll(checkName, pass ? "true" : "false");

            DebugWriteCheckTranscriptText(textName, text, checkName, checking!, pass);
        }

        private void CheckTranscriptTextContains(string textName, string text, string check)
        {
            var checkName = "check.sr.transcript." + check + ".contains";
            var checking = _values[checkName];

            var checkItems = checking?.Split(';', '\r', '\n');
            var pass = checkItems?.Count(x => text.Contains(x)) == checkItems?.Count();
            if (!pass) SetPassed(false);

            _values.Add("output.all." + checkName, "true");
            EnsureCacheAll(checkName, pass ? "true" : "false");

            DebugWriteCheckTranscriptText(textName, text, checkName, checking!, pass);
        }

        private void CheckTranscriptTextNotIn(string textName, string text, string check)
        {
            var checkName = "check.sr.transcript." + check + ".not.in";
            var checking = _values[checkName];

            var pass = checking?.Split(';', '\r', '\n')?.Count(x => x.CompareTo(text) == 0) == 0;
            if (!pass) SetPassed(false);

            _values.Add("output.all." + checkName, "true");
            EnsureCacheAll(checkName, pass ? "true" : "false");

            DebugWriteCheckTranscriptText(textName, text, checkName, checking!, pass);
        }

        private void CheckTranscriptTextNotContains(string textName, string text, string check)
        {
            var checkName = "check.sr.transcript." + check + ".not.contains";
            var checking = _values[checkName];

            var pass = checking?.Split(';', '\r', '\n')?.Count(x => text.Contains(x)) == 0;
            if (!pass) SetPassed(false);

            _values.Add("output.all." + checkName, "true");
            EnsureCacheAll(checkName, pass ? "true" : "false");

            DebugWriteCheckTranscriptText(textName, text, checkName, checking!, pass);
        }

        private void DebugWriteCheckTranscriptWer(string transcriptName, string transcript, string textName, string text, string checkName, string checking, bool pass)
        {
            if (!_debugOutput) return;
            Console.WriteLine($"{transcriptName}={transcript}\n{textName}={text}\n{checkName}={checking}\npass={pass}");
        }

        private void DebugWriteCheckTranscriptText(string textName, string text, string checkName, string checking, bool pass)
        {
            if (!_debugOutput) return;
            Console.WriteLine($"{textName}={text}\n{checkName.Replace('\r', ';').Replace('\n', ';')}={checking}\npass={pass}");
        }

        private void SetPassed(bool passed)
        {
            if (!passed && _values.GetOrDefault("passed", true))
            {
                _values.Reset("passed", "false");
            }
        }

        private void FlushOutputZipFile()
        {
            if (!_outputAll && !_outputEach && !_outputBatch) return;

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
                    if (_outputAll) AddToZip(archive, _outputAllFileName!);
                    if (_outputEach) AddToZip(archive, _outputEachFileName!);
                    if (_outputBatch) AddToZip(archive, _outputBatchFileName!);
                    if (_outputVtt) AddToZip(archive, _outputVttFileName!);
                    if (_outputSrt) AddToZip(archive, _outputSrtFileName!);
                }
            });
        }

        private string CreateTimestamp()
        {
            return String.Format("{0:yyyy-MM-dd hh:mm:ss.ffff}", DateTime.UtcNow);
        }

        private void AddToZip(ZipArchive zip, string file)
        {
            var name = (new FileInfo(file)).Name;

            var entry = zip.GetEntry(name);
            if (entry != null) entry.Delete();

            zip.CreateEntryFromFile(file, name);
        }

        private ICommandValues _values;
        private bool _verbose;

        private SpinLock _lock = new SpinLock();

        private bool _debugOutput = false;

        private bool _outputBatch = false;
        private string? _outputBatchFileName = null;

        private bool _outputVtt = false;
        private string? _outputVttFileName = null;

        private bool _outputSrt = false;
        private string? _outputSrtFileName = null;

        private bool _cacheResults = false;

        private List<object>? _outputResultCache = null;
        private Dictionary<string, string>? _propertyCache = null;
        private Dictionary<string, PropertyCollection>? _propertyCollectionCache = null;

        private bool _outputAll = false;
        private string? _outputAllFileName = null;
        private string? _outputAllFileType = null;
        private Dictionary<string, List<string>>? _outputAllCache;
        private Dictionary<string, char>? _outputAllSeparatorCache;

        private bool _outputEach = false;
        private bool _overwriteEach = false;
        private string? _outputEachFileName = null;
        private string? _outputEachFileType = null;
        private Dictionary<string, string>? _outputEachCache;
        private List<Dictionary<string, string>>? _outputEachCache2;

        private Dictionary<string, List<string>>? _checkAllCache;

        private CultureInfo? _sourceCulture;
        private CultureInfo? _targetCulture;

    }
}
