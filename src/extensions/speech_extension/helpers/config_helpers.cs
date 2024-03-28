//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

namespace Azure.AI.Details.Common.CLI
{
    class ConfigHelpers
    {
        public static SpeechConfig CreateSpeechConfig(ICommandValues values, string? key = null, string? region = null)
        {
            key = string.IsNullOrEmpty(key) ? values["service.config.key"] : key;
            region = string.IsNullOrEmpty(region) ? values["service.config.region"] : region;
            var host = values["service.config.host"];
            var endpoint = values["service.config.endpoint.uri"];
            var tokenValue = values["service.config.token.value"];

            if (values.Contains("embedded.config.embedded"))
            {
                key = "UNUSED";
                region = "UNUSED";
            }

            if (string.IsNullOrEmpty(endpoint) && string.IsNullOrEmpty(region) && string.IsNullOrEmpty(host))
            {
                values.AddThrowError("ERROR:", $"Creating SpeechConfig; requires one of: region, endpoint, or host.");
            }
            else if (!string.IsNullOrEmpty(region) && string.IsNullOrEmpty(tokenValue) && string.IsNullOrEmpty(key))
            {
                values.AddThrowError("ERROR:", $"Creating SpeechConfig; use of region requires one of: key or token.");
            }

            SpeechConfig config;
            if (!string.IsNullOrEmpty(endpoint))
            {
                config = string.IsNullOrEmpty(key)
                    ? SpeechConfig.FromEndpoint(new Uri(endpoint))
                    : SpeechConfig.FromEndpoint(new Uri(endpoint), key);
            }
            else if (!string.IsNullOrEmpty(host))
            {
                config = string.IsNullOrEmpty(key)
                    ? SpeechConfig.FromHost(new Uri(host))
                    : SpeechConfig.FromHost(new Uri(host), key);
            }
            else // if (!string.IsNullOrEmpty(region))
            {
                config = string.IsNullOrEmpty(tokenValue)
                    ? SpeechConfig.FromSubscription(key, region)
                    : SpeechConfig.FromAuthorizationToken(tokenValue, region);
            }

            if (!string.IsNullOrEmpty(tokenValue))
            {
                config.AuthorizationToken = tokenValue;
            }

            SetupLogFile(config, values);

            var stringProperty = values.GetOrEmpty("config.string.property");
            if (!string.IsNullOrEmpty(stringProperty)) ConfigHelpers.SetStringProperty(config, stringProperty);

            var stringProperties = values.GetOrEmpty("config.string.properties");
            if (!string.IsNullOrEmpty(stringProperties)) ConfigHelpers.SetStringProperties(config, stringProperties);

            return config;
        }

        public static AudioConfig CreateAudioConfig(ICommandValues values)
        {
            var input = values["audio.input.type"];
            var file = values["audio.input.file"];
            var format = values["audio.input.format"];
            var device = values["audio.input.microphone.device"];

            AudioConfig? audioConfig = null;
            if (input == "microphone" || string.IsNullOrEmpty(input))
            {
                audioConfig = AudioHelpers.CreateMicrophoneAudioConfig(device);
            }
            else if (input == "file" && !string.IsNullOrEmpty(file))
            {
                file = FileHelpers.DemandFindFileInDataPath(file, values, "audio input");
                audioConfig = AudioHelpers.CreateAudioConfigFromFile(file, format);
            }
            else
            {
                values.AddThrowError("WARNING:", $"'audio.input.type={input}' NOT YET IMPLEMENTED!!");
            }

            return audioConfig!;
        }

        public static void SetEndpointParams(SpeechConfig config, string endpointParams)
        {
            var queryParams = System.Web.HttpUtility.ParseQueryString(endpointParams);
            foreach(var param in queryParams)
            {
                var paramName = param.ToString();
                config.SetServiceProperty(paramName, queryParams[paramName], ServicePropertyChannel.UriQueryParameter);
            }
        }

        public static void SetStringProperties(SpeechConfig config, string properties)
        {
            var lines = properties.Split('\r', '\n', ';');
            foreach (var line in lines)
            {
                SetStringProperty(config, line);
            }
        }

        public static void SetStringProperty(SpeechConfig config, string property)
        {
            if (StringHelpers.SplitNameValue(property, out string name, out string value))
            {
                if (Enum.TryParse<PropertyId>(name, true, out PropertyId id))
                {
                    config.SetProperty(id, value);
                }
                else
                {
                    config.SetProperty(name, value);
                }
            }
        }

        public static void SetupLogFile(SpeechConfig config, ICommandValues values)
        {
            var log = values["diagnostics.config.log.file"];
            if (!string.IsNullOrEmpty(log))
            {
                var id = values.GetOrEmpty("audio.input.id");
                if (log.Contains("{id}")) log = log.Replace("{id}", id);

                var pid = Process.GetCurrentProcess().Id.ToString();
                if (log.Contains("{pid}")) log = log.Replace("{pid}", pid);

                var time = DateTime.Now.ToFileTime().ToString();
                if (log.Contains("{time}")) log = log.Replace("{time}", time);

                var runTime = values.GetOrEmpty("x.run.time");
                if (log.Contains("{run.time}")) log = log.Replace("{run.time}", runTime);

                log = FileHelpers.GetOutputDataFileName(log, values);
                config.SetProperty(PropertyId.Speech_LogFilename, log);
            }
        }

    }
}
