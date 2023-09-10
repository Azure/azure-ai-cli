//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Linq;
using System.Collections.Generic;
using Azure.AI.Details.Common.CLI.ConsoleGui;
using System.Text;

namespace Azure.AI.Details.Common.CLI
{
    public class NameGenHelper
    {
        public static string GenerateName()
        {
            EnsureLoaded();

            var adjective = GetRandomElement(_adjectives);
            var color = GetRandomElement(_colors);
            var animal = GetRandomElement(_animals);

            return $"{adjective}-{color}-{animal}";
        }

        private static void EnsureLoaded()
        {
            if (_adjectives == null)
            {
                _adjectives = LoadFrom("adjectives.txt");
            }

            if (_colors == null)
            {
                _colors = LoadFrom("colors.txt");
            }

            if (_animals == null)
            {
                _animals = LoadFrom("animals.txt");
            }
        }

        private static string[] LoadFrom(string fileName)
        {
            fileName = $"help/include.text.{fileName}";
            fileName = FileHelpers.FindFileInHelpPath(fileName);

            var text = !string.IsNullOrEmpty(fileName)
                ? FileHelpers.ReadAllHelpText(fileName, Encoding.UTF8)
                : throw new ApplicationException($"Could not find file '{fileName}'.");

            return text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static string GetRandomElement(string[] array)
        {
            int index = _random.Next(array.Length);
            return array[index];
        }

        private static string[] _adjectives = null;
        private static string[] _colors = null;
        private static string[] _animals = null;

        private static readonly Random _random = new Random();
    }

    public class NamePickerHelper
    {
        public static string DemandPickOrEnterName(string namePrompt, string nameOutKind, string nameIn = null, string nameInKind = null)
        {
            var choices = GetNameChoices(nameIn, nameInKind, nameOutKind);
            var usePicker = choices != null && choices.Count() > 1;

            if (usePicker)
            {
                Console.Write(namePrompt);
                var pick = ListBoxPicker.PickIndexOf(choices);
                if (pick < 0) ThrowPromptNotAnsweredApplicationException();

                Console.Write("\r");

                var pickedUseCustomName = pick == choices.Length - 1;
                if (!pickedUseCustomName)
                {
                    Console.WriteLine($"{namePrompt}{choices[pick]}");
                    return choices[pick];
                }
            }

            return DemandAskPrompt(namePrompt);
        }

        private static string[] GetNameChoices(string nameIn, string nameInKind, string nameOutKind)
        {
            var choices = new List<string>();

            var nameInOk = !string.IsNullOrEmpty(nameIn) && !string.IsNullOrEmpty(nameInKind);
            if (nameInOk && nameIn.StartsWith($"{nameInKind}-"))
            {
                var nameBase = nameIn.Substring(nameInKind.Length + 1);
                choices.Add($"{nameOutKind}-{nameBase}");
            }

            if (nameInOk && nameIn.EndsWith($"-{nameInKind}"))
            {
                var nameBase = nameIn.Substring(0, nameIn.Length - nameInKind.Length - 1);
                choices.Add($"{nameBase}-{nameOutKind}");
            }

            if (nameInOk && nameIn.Contains($"-{nameInKind}-"))
            {
                var nameBase = nameIn.Replace($"-{nameInKind}-", $"-{nameOutKind}-");
                choices.Add(nameBase);
            }

            if (nameInOk && choices.Count() == 0)
            {
                choices.Add($"{nameIn}-{nameOutKind}");
                choices.Add($"{nameOutKind}-{nameIn}");
            }

            if (!nameInOk)
            {
                var name = NameGenHelper.GenerateName();
                choices.Add($"{nameOutKind}-{name}");
                choices.Add($"{name}-{nameOutKind}");
            }

            choices.Sort();
            choices.Add("(Enter custom name)");

            return choices.ToArray();
        }

        private static string DemandAskPrompt(string prompt, string value = null, bool useEditBox = false)
        {
            var answer = AskPromptHelper.AskPrompt(prompt, value, useEditBox);
            if (string.IsNullOrEmpty(answer))
            {
                ThrowPromptNotAnsweredApplicationException();
            }
            return answer;
        }

        private static void ThrowPromptNotAnsweredApplicationException()
        {
            throw new ApplicationException($"CANCELED: No input provided.");
        }
    }
}
