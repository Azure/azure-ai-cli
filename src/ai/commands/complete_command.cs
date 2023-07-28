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
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using Azure.AI.OpenAI;

namespace Azure.AI.Details.Common.CLI
{
    public class CompleteCommand : Command
    {
        internal CompleteCommand(ICommandValues values)
        {
            _values = values.ReplaceValues();
        }

        internal bool RunCommand()
        {
            Complete();
            return _values.GetOrDefault("passed", true);
        }

        private void Complete()
        {
            StartCommand();

            var kind = _values["complete.input.type"];
            switch (kind)
            {
                case "":
                case null:
                case "interactive":
                    // SynthesizeInteractive(false);
                    // break;
                    
                case "interactive+":
                    CompleteInteractively(true);
                    break;

                // TODO: Add support for other input types
            }

            StopCommand();
            DisposeAfterStop();
            DeleteTemporaryFiles();
        }

        private void CompleteInteractively(bool repeatedly = false)
        {
            var client = CreateOpenAIClient(out var deployment);
            var options = CreateCompletionOptions();

            while (true)
            {
                Console.Write("[complete] >>> ");
                var text = Console.ReadLine();

                if (text.ToLower() == "") break;
                if (text.ToLower() == "stop") break;
                if (text.ToLower() == "quit") break;
                if (text.ToLower() == "exit") break;

                var task = GetCompletionsAsync(client, deployment, options, text);
                WaitForStopOrCancel(task);

                if (!repeatedly) break;
                if (_canceledEvent.WaitOne(0)) break;
            }
        }

        private async Task<Response<Completions>> GetCompletionsAsync(OpenAIClient client, string deployment, CompletionsOptions options, string text)
        {
            options.Prompts.Clear();
            options.Prompts.Add(text);
            var response = await client.GetCompletionsAsync(deployment, options);

            Console.WriteLine();
            Console.WriteLine(response.Value.Choices[0].Text);
            Console.WriteLine();

            return response;
        }

        private CompletionsOptions CreateCompletionOptions()
        {
            var options = new CompletionsOptions();
            // messages.ToList().ForEach(m => options.Messages.Add(m));

            // options.MaxTokens = TryParse(maxTokens, _defaultMaxTokens);
            // options.Temperature = TryParse(temperature, _defaultTemperature);
            // options.FrequencyPenalty = TryParse(frequencyPenalty, _defaultFrequencyPenalty);
            // options.PresencePenalty = TryParse(presencePenalty, _defaultPresencePenalty);

            // if (!string.IsNullOrEmpty(stop))
            // {
            //     var stops = stop.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
            //     stops.ForEach(s => options.StopSequences.Add(s));
            // }

            return options;
        }

        private OpenAIClient CreateOpenAIClient(out string deployment)
        {
            var key = _values["service.config.key"];
            var host = _values["service.config.host"];
            var region = _values["service.config.region"];
            var endpoint = _values["service.config.endpoint.uri"];
            var tokenValue = _values["service.config.token.value"];

            deployment = _values["service.config.deployment"];

            if (string.IsNullOrEmpty(endpoint) && string.IsNullOrEmpty(region) && string.IsNullOrEmpty(host))
            {
                _values.AddThrowError("ERROR:", $"Creating OpenAIClient; requires one of: region, endpoint, or host.");
            }
            else if (!string.IsNullOrEmpty(region) && string.IsNullOrEmpty(tokenValue) && string.IsNullOrEmpty(key))
            {
                _values.AddThrowError("ERROR:", $"Creating OpenAIClient; use of region requires one of: key or token.");
            }
            else if (string.IsNullOrEmpty(deployment))
            {
                _values.AddThrowError("ERROR:", $"Creating OpenAIClient; requires deployment.");
            }

            if (!string.IsNullOrEmpty(endpoint))
            {
                return new OpenAIClient(
                    new Uri(endpoint!),
                    new AzureKeyCredential(key!));
            }
            else if (!string.IsNullOrEmpty(host))
            {
                _values.AddThrowError("ERROR:", $"Creating OpenAIClient; Not-yet-implemented create from host.");
                return null;
            }
            else // if (!string.IsNullOrEmpty(region))
            {
                _values.AddThrowError("ERROR:", $"Creating OpenAIClient; Not-yet-implemented create from region.");
                return null;
            }
        }

        private void WaitForStopOrCancel(Task<Response<Completions>> task)
        {
            var interval = 100;

            while (!task.Wait(interval))
            {
                if (_stopEvent.WaitOne(0)) break;
                if (_canceledEvent.WaitOne(0)) break;
            }
        }

        private void StartCommand()
        {
            CheckPath();
            // CheckCompleteInput();

            // _display = new DisplayHelper(_values);

            // _output = new OutputHelper(_values);
            // _output.StartOutput();

            // var id = _values["complete.input.id"];
            // _output.EnsureOutputAll("complete.input.id", id);
            // _output.EnsureOutputEach("complete.input.id", id);

            _lock = new SpinLock();
            _lock.StartLock();
        }

        private void StopCommand()
        {
            _lock.StopLock(5000);
            _stopEvent.Set();

            // _output.CheckOutput();
            // _output.StopOutput();
        }

        private SpinLock _lock = null;

        // OutputHelper _output = null;
        // DisplayHelper _display = null;
    }
}
