//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Speaker;
using System.Text.Json;

namespace Azure.AI.Details.Common.CLI
{
    public class ProfileCommand : Command
    {
        public ProfileCommand(ICommandValues values)
        {
            _values = values.ReplaceValues();
            _quiet = _values.GetOrDefault("x.quiet", false);
            _verbose = _values.GetOrDefault("x.verbose", true);
        }

        public bool RunCommand()
        {
            try
            {
                RunSpeakerCommand();
            }
            catch (WebException ex)
            {
                ConsoleHelpers.WriteLineError($"\n  ERROR: {ex.Message}");
                JsonHelpers.PrintJson(HttpHelpers.ReadWriteJson(ex.Response, _values, "profile"));
            }

            return _values.GetOrDefault("passed", true);
        }

        private bool RunSpeakerCommand()
        {
            DoCommand(_values.GetCommand());
            return _values.GetOrDefault("passed", true);
        }

        private void DoCommand(string command)
        {
            CheckPath();

            switch (command.Replace("speech.", ""))
            {
                case "profile.list": DoList(); break;
                case "profile.create": DoCreateProfile(); break;
                case "profile.delete": DoDeleteProfile(); break;
                case "profile.status": DoProfileStatus(); break;
                case "profile.enroll": DoEnrollProfile(); break;

                case "speaker.identify": Recognize(VoiceProfileType.TextIndependentIdentification); break; 
                case "speaker.verify": Recognize(); break;

                default:
                    _values.AddThrowError("WARNING:", $"'{command.Replace('.', ' ')}' NOT YET IMPLEMENTED!!");
                    break;
            }
        }

        private void DoList()
        {
            string message = "getting profile list...";

            if (!_quiet) Console.WriteLine(message);

            using (var client = GetVoiceProfileClient())
            {
                var kind = GetVoiceProfileType();
                var profiles = client.GetAllProfilesAsync(kind).Result;
                if (!_quiet) Console.WriteLine($"{message} Done!\n");
                var json = ReadWritePrintObjectJson(profiles);
                CheckWriteOutputIds(json, "Id");
                WriteJsonToFile(json);
            }
        }

        private void WriteJsonToFile(string json)
        {
            var saveAs = _values.GetOrDefault("profile.output.file", null);
            if (saveAs != null) 
            { 
              saveAs = FileHelpers.GetOutputDataFileName(saveAs, _values);
              var saveMessage = $"Saving as {saveAs} ...";
              if (!_quiet) Console.WriteLine(saveMessage);
              FileHelpers.WriteAllText(saveAs, json, new UTF8Encoding(false));
              if (!_quiet) Console.WriteLine($"{saveMessage} Done!\n");
            } 
        }

        private void DoCreateProfile()
        {
            var message = $"Creating profile ...";
            if (!_quiet) Console.WriteLine(message);
            using (var client = GetVoiceProfileClient())
            {
                var kind = GetVoiceProfileType();
                var language = _values.GetOrDefault("profile.source.language", "en-US");
                var profile = client.CreateProfileAsync(kind, language).Result;
                if (!_quiet) Console.WriteLine($"{message} Done!\n");
                var json = ReadWritePrintObjectJson(profile);
                CheckWriteOutputSelf(json, "Id");
            }
        }

        private void DoDeleteProfile()
        {
            var profileId = _values.GetOrDefault("profile.id", "");
            var file = _values.GetOrDefault("profile.input.file", "");
            if (string.IsNullOrEmpty(profileId) && string.IsNullOrEmpty(file))
            {
                _values.AddThrowError(
                    "WARNING:", $"Cannot delete profile; id or file input not found!",
                                "",
                        "USE:", $"{Program.Name} profile delete --id ID",
                                "",
                        "SEE:", $"{Program.Name} help profile delete");
            }

            var ids = new List<string> { profileId };
            if(!string.IsNullOrEmpty(file))
            {
                ids = GetAllIdsFromFile(file, "Id");
            }

            foreach (string id in ids)
            { 
              var message = $"Deleting profile '{id}' ...";
              if (!_quiet) Console.WriteLine(message);
              using (var client = GetVoiceProfileClient())
              {
                  var kind = GetVoiceProfileType();
                  var profile = new VoiceProfile(id, kind);
                  var result = client.DeleteProfileAsync(profile).Result;
                  if (!_quiet) Console.WriteLine($"{message} Done!\n");
                  if (result.Reason == ResultReason.Canceled)
                  {
                      ReadWritePrintResultJson(getCancellationDetails(result), result.Reason.ToString());
                  }
                  else
                  { 
                      ReadWritePrintResultJson(result, result.Reason.ToString());
                  }    
              }
            }
        }

        private List<string>? GetAllIdsFromFile(string profilesList, string key)
        {
            var parsed = JsonDocument.Parse(profilesList);
            var root = parsed.RootElement;

            if (root.ValueKind == JsonValueKind.Array)
            {
                return ParseArray(root.EnumerateArray().ToArray(), key);
            }
            else if (root.TryGetProperty("value", out var value))
            {
                return ParseArray(value.EnumerateArray().ToArray(), "profileId");
            }
            else if (root.TryGetProperty("profiles", out var profiles))
            {
                return ParseArray(profiles.EnumerateArray().ToArray(), "profileId");
            }
            return null;
        }

        private List<string> ParseArray(JsonElement[] array, string key)
        {
            var ids = new List<string>{}; 
            foreach (var token in array)
            {
                var id = token.GetPropertyStringOrNull(key);
                ids.Add(id); 
            }

            return ids;
        }

        private void DoProfileStatus()
        {
            var id = _values.GetOrDefault("profile.id", "");
            if (string.IsNullOrEmpty(id))
            {
                _values.AddThrowError(
                    "WARNING:", $"Cannot get status; id not found!",
                                "",
                        "USE:", $"{Program.Name} profile status --id ID [...]",
                                "",
                        "SEE:", $"{Program.Name} help profile status");
            }

            var message = $"Getting status for profile {id} ...";
            if (!_quiet) Console.WriteLine(message);
            using (var client = GetVoiceProfileClient())
            {
                var kind = GetVoiceProfileType();
                var profile = new VoiceProfile(id, kind);
                var result = client.RetrieveEnrollmentResultAsync(profile).Result;
                if (!_quiet) Console.WriteLine($"{message} Done!\n");
                string json;
                if (result.Reason == ResultReason.Canceled)
                {
                    json = ReadWritePrintResultJson(getCancellationDetails(result), result.Reason.ToString());
                }
                else
                { 
                    json = ReadWritePrintResultJson(result, result.Reason.ToString());
                }    
                CheckWriteOutputSelf(json, "ProfileId");
                WriteJsonToFile(json);
            }
        }

        private VoiceProfileClient GetVoiceProfileClient()
        {
            var config = ConfigHelpers.CreateSpeechConfig(_values);
            return new VoiceProfileClient(config);
        }

        private VoiceProfileType GetVoiceProfileType()
        {
            var kind = _values.GetOrDefault("profile.kind", "TextIndependentIdentification");
            return kind.Equals("TextDependentVerification", StringComparison.OrdinalIgnoreCase) ?
                VoiceProfileType.TextDependentVerification : kind.Equals("TextIndependentVerification", StringComparison.OrdinalIgnoreCase) ?
                VoiceProfileType.TextIndependentVerification : VoiceProfileType.TextIndependentIdentification;
        }

        private void DoEnrollProfile()
        {
            var id = _values.GetOrDefault("profile.id", "");
            if (string.IsNullOrEmpty(id))
            {
                _values.AddThrowError(
                    "WARNING:", $"Cannot enroll profile; id not found!",
                                "",
                        "USE:", $"{Program.Name} profile enroll --id ID --file FILE [...]",
                                "",
                        "SEE:", $"{Program.Name} help profile enroll");

            }

            var audioConfig = GetAudioConfig();
            var input = _microphone ? "microphone" : _file;
            var message = $"Enrolling profile {id} from {input}...";
            if (!_quiet) Console.WriteLine(message);
            using (var client = GetVoiceProfileClient())
            {
                var kind = GetVoiceProfileType();
                var profile = new VoiceProfile(id, kind);
                var result = client.EnrollProfileAsync(profile, audioConfig).Result;
                if (!_quiet) Console.WriteLine($"{message} Done!\n");
                string json;
                if (result.Reason == ResultReason.Canceled)
                {
                    json = ReadWritePrintResultJson(getCancellationDetails(result), result.Reason.ToString());
                }
                else
                { 
                    json = ReadWritePrintResultJson(result, result.Reason.ToString());
                }    
                WriteJsonToFile(json);
            }
        }

        private void Recognize(VoiceProfileType type = VoiceProfileType.TextIndependentVerification)
        {
            var id = _values.GetOrDefault("profile.id", "");
            RecognizeSpeaker(type, id);
        }

        private void RecognizeSpeaker(VoiceProfileType type, string id)
        {
            var kind = GetVoiceProfileType();
            if(!areTypesCompatible(type, kind))
            { 
                _values.AddThrowError("WARNING:", $"Wrong VoiceProfile type {type} for given operation;");
            }
            if (string.IsNullOrEmpty(id))
            {
                var command = _values.GetCommandForDisplay();
                _values.AddThrowError(
                    "WARNING:", $"Cannot invoke recognize operation on audio; id(s) not found!",
                                "",
                        "USE:", $"{Program.Name} {command} --id ID --file FILE [...]",
                                "",
                        "SEE:", $"{Program.Name} help speaker id");
            }

            var audioConfig = GetAudioConfig();

            var idList = id.Split(',', ';', '\r', '\n');
            var voiceProfiles = new List<VoiceProfile>();
            foreach (var profileId in idList)
            {
                voiceProfiles.Add(new VoiceProfile(profileId, kind));
            }
            var config = ConfigHelpers.CreateSpeechConfig(_values);
            var speakerRecognizer = new SpeakerRecognizer(config, audioConfig);
            var input = _microphone ? "microphone" : _file;
            var message = $"invoking recognizeOnceAsync on {input} against id(s) {id} ...";
            if (!_quiet) Console.WriteLine(message);

            SpeakerRecognitionResult result = (kind == VoiceProfileType.TextIndependentIdentification) ? 
                speakerRecognizer.RecognizeOnceAsync(SpeakerIdentificationModel.FromProfiles(voiceProfiles)).Result :
                speakerRecognizer.RecognizeOnceAsync(SpeakerVerificationModel.FromProfile(voiceProfiles[0])).Result;

            if (!_quiet) Console.WriteLine($"{message} Done!\n");

            string json;
            if (result.Reason == ResultReason.Canceled)
            {
                json = ReadWritePrintResultJson(getCancellationDetails(result), result.Reason.ToString());
            }
            else
            { 
                json = ReadWritePrintResultJson(result, result.Reason.ToString());
            }    
            WriteJsonToFile(json);
        }

        private bool areTypesCompatible(VoiceProfileType type1, VoiceProfileType type2)
        {
            switch (type1) 
            {
                case VoiceProfileType.TextDependentVerification:
                case VoiceProfileType.TextIndependentVerification:
                    return type2 != VoiceProfileType.TextIndependentIdentification;
                case VoiceProfileType.TextIndependentIdentification:
                    return type2 == VoiceProfileType.TextIndependentIdentification;
            }
            return false;
        }
        private object getCancellationDetails(object result)
        {
            switch (result.GetType().Name.ToString())
            {
                case "SpeakerRecognitionResult":
                    return SpeakerRecognitionCancellationDetails.FromResult((SpeakerRecognitionResult)result);
                case "VoiceProfileResult":
                    return VoiceProfileCancellationDetails.FromResult((VoiceProfileResult)result);
                case "VoiceProfileEnrollmentResult":
                    return VoiceProfileEnrollmentCancellationDetails.FromResult((VoiceProfileEnrollmentResult)result);
                case "VoiceProfilePhraseResult":
                    return VoiceProfilePhraseCancellationDetails.FromResult((VoiceProfilePhraseResult)result);
                default:
                    return "";
            }
        }

        private string ReadWritePrintResultJson(object result, string resultReason)
        {
            var resultJson = System.Text.Json.JsonSerializer.Serialize(result);
            var resultParsed = JsonDocument.Parse(resultJson);

            var reasonJson = $"{{\"Reason\": \"{resultReason}\"}}";
            var reasonParsed = JsonDocument.Parse(reasonJson);

            var json = JsonHelpers.MergeJsonObjects(resultParsed.RootElement, reasonParsed.RootElement); 
            FileHelpers.CheckOutputJson(json, _values, "profile");

            if (!_quiet && _verbose) JsonHelpers.PrintJson(json);

            return json;
        }

        private string ReadWritePrintObjectJson(object o)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(o) + Environment.NewLine;
            FileHelpers.CheckOutputJson(json, _values, "profile");

            if (!_quiet && _verbose) JsonHelpers.PrintJson(json);

            return json;
        }

        private void CheckWriteOutputId(string id)
        {
            var idOk = !string.IsNullOrEmpty(id);

            var atId = _values.GetOrDefault("profile.output.id", "");
            var atIdOk = !string.IsNullOrEmpty(atId);
            if (idOk && atIdOk)
            {
                var atIdFile = FileHelpers.GetOutputDataFileName(atId, _values);
                FileHelpers.WriteAllText(atIdFile, id, Encoding.UTF8);
            }

            var addId = _values.GetOrDefault("profile.output.add.id", "");
            var addIdOk = !string.IsNullOrEmpty(addId);
            if (idOk && addIdOk)
            {
                var addIdFile = FileHelpers.GetOutputDataFileName(addId, _values);
                FileHelpers.AppendAllText(addIdFile, "\n" + id, Encoding.UTF8);
            }
        }

        private string CheckWriteOutputSelf(string json, string key)
        {
            var parsed = JsonDocument.Parse(json);
            var id = parsed.GetPropertyStringOrNull(key);
            CheckWriteOutputId(id);
            return id;
        }

        private void CheckWriteOutputIds(string json, string key)
        {
            var atIds = _values.GetOrDefault("profile.output.ids", "");
            var atIdsOk = !string.IsNullOrEmpty(atIds);
            var ids = atIdsOk ? new StringBuilder() : null;

            var outputLast = _values.GetOrDefault("profile.output.list.last", false);
            if (!atIdsOk && !outputLast) return;

            var parsed = JsonDocument.Parse(json);
            var items = parsed.GetPropertyArrayOrEmpty("profiles");
            foreach (var item in items)
            {
                var id = item.GetPropertyStringOrNull(key);
                if (ids != null) ids.AppendLine(id); 
            } 
            if (ids != null) 
            { 
                var atIdsFile = FileHelpers.GetOutputDataFileName(atIds, _values);
                FileHelpers.WriteAllText(atIdsFile, ids.ToString(), Encoding.UTF8);
            }

            if (outputLast && items.Count() > 0)
            {
                var item = items.Last();
                var id = item.GetPropertyStringOrNull(key);
                CheckWriteOutputId(id);
            }
        }

        private AudioConfig GetAudioConfig()
        {
            CheckAudioInput();
            AudioConfig audioConfig;
            if (_microphone)
            {
              audioConfig = AudioConfig.FromDefaultMicrophoneInput();
            }
            else
            {
                _file = _values.GetOrDefault("profile.input.file", null);
                if (string.IsNullOrEmpty(_file))
                {
                    var command = _values.GetCommandForDisplay();
                    var action = _values.GetCommand().Split('.').LastOrDefault();
                    _values.AddThrowError(
                        "WARNING:", $"Cannot {action}; file not found!",
                                    "",
                            "USE:", $"{Program.Name} {command} --id ID --file FILE [...]",
                                    "",
                            "SEE:", $"{Program.Name} help {command}");
                }
                audioConfig = AudioHelpers.CreateAudioConfigFromFile(_file, null);
            }
            return audioConfig;
        }

        private void CheckAudioInput()
        {
            var input = _values["profile.input.type"];
            _microphone = (input == "microphone" || string.IsNullOrEmpty(input));
        }

        protected bool _microphone = false;
        protected string _file = "";

        private bool _quiet = false;
        private bool _verbose = false;
    }
}
