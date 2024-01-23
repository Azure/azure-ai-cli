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
        public static string GenerateName(string userName = null, int maxCch = 24)
        {
            EnsureLoaded();

            userName = userName ?? Environment.UserName;
            userName = userName?.Trim().Replace("_", "-");
            userName = userName?.Split(new[] { '@' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

            var approaches = 8;
            var maxTries = 1000;

            for (int approach = 0; approach < approaches; approach++)
            {
                if (approach < approaches / 2 && string.IsNullOrEmpty(userName)) continue;

                for (int i = 0; i < maxTries; i++)
                {
                    var animal = GetRandomElement(_animals);
                    var color = GetRandomElement(_colors);
                    var adjective = GetRandomElement(_adjectives);

                    var name = approach switch
                    {
                        0 => $"{userName}-{adjective}-{color}-{animal}",
                        1 => $"{userName}-{adjective}-{animal}",
                        2 => $"{userName}-{color}-{animal}",
                        3 => $"{userName}-{animal}",
                        4 => $"{adjective}-{color}-{animal}",
                        5 => $"{adjective}-{animal}",
                        6 => $"{color}-{animal}",
                        7 => $"{animal}",

                        _ => throw new ApplicationException($"Unexpected approach '{approach}'."),
                    };

                    if (name.Length <= maxCch) return name;
                }
            }

            return Guid.NewGuid().ToString().Substring(0, maxCch);
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
        public static string DemandPickOrEnterName(string namePrompt, string nameOutKind, string nameIn = null, string nameInKind = null, string userName = null, int maxCch = 32)
        {
            var choices = GetNameChoices(nameIn, nameInKind, nameOutKind, userName, maxCch);
            var usePicker = choices != null && choices.Count() > 1;

            while (usePicker)
            {
                choices.Insert(0, "(Regenerate choices)");

                Console.Write(namePrompt);
                var pick = ListBoxPicker.PickIndexOf(choices.ToArray());
                if (pick < 0) ThrowPromptNotAnsweredApplicationException();

                Console.Write("\r");

                if (pick == 0)
                {
                    choices = GetNameChoices(null, null, nameOutKind, userName, maxCch);
                    continue;
                }

                var pickedUseCustomName = pick == choices.Count() - 1;
                if (!pickedUseCustomName)
                {
                    Console.WriteLine($"{namePrompt}{choices[pick]}");
                    return choices[pick];
                }

                break;
            }

            while (true)
            {
                var name = DemandAskPrompt(namePrompt);
                if (name.Length > maxCch)
                {
                    Console.WriteLine($"*** WARNING: Name is too long. Max length is {maxCch}.\n");
                    continue;
                }

                if (name.Count(x => !char.IsLetterOrDigit(x) && x != '-') > 0)
                {
                    Console.WriteLine($"*** WARNING: Name contains invalid characters. Only letters, digits, and dashes are allowed.\n");
                    continue;
                }

                return name;
            }
        }

        private static List<string> GetNameChoices(string nameIn, string nameInKind, string nameOutKind, string userName, int maxCch)
        {
            var choices = new List<string>();

            var nameInOk = !string.IsNullOrEmpty(nameIn) && !string.IsNullOrEmpty(nameInKind);
            if (nameInOk)
            {
                nameIn = nameIn.Trim().Replace("_", "-");
            }

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

            choices = choices.Where(x => x.Length <= maxCch).ToList();

            if (!nameInOk || choices.Count() == 0)
            {
                var name = NameGenHelper.GenerateName(userName, maxCch - 1 - nameOutKind.Length);
                choices.Add($"{nameOutKind}-{name}");
                choices.Add($"{name}-{nameOutKind}");
            }

            choices.Sort();
            choices.Add("(Enter custom name)");

            return choices;
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
