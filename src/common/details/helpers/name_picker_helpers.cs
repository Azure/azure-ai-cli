//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Linq;
using System.Collections.Generic;
using Azure.AI.Details.Common.CLI.ConsoleGui;

namespace Azure.AI.Details.Common.CLI
{
    public class NamePickerHelper
    {
        public static string DemandPickOrEnterName(string namePrompt, string nameIn, string nameInKind, string nameOutKind)
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
            if (string.IsNullOrEmpty(nameIn)) return null;

            var choices = new List<string>();
            if (nameIn.StartsWith($"{nameInKind}-"))
            {
                var nameBase = nameIn.Substring(nameInKind.Length + 1);
                choices.Add($"{nameOutKind}-{nameBase}");
            }

            if (nameIn.EndsWith($"-{nameInKind}"))
            {
                var nameBase = nameIn.Substring(0, nameIn.Length - nameInKind.Length - 1);
                choices.Add($"{nameBase}-{nameOutKind}");
            }

            if (nameIn.Contains($"-{nameInKind}-"))
            {
                var nameBase = nameIn.Replace($"-{nameInKind}-", $"-{nameOutKind}-");
                choices.Add(nameBase);
            }

            if (choices.Count() == 0)
            {
                choices.Add($"{nameIn}-{nameOutKind}");
                choices.Add($"{nameOutKind}-{nameIn}");
            }

            choices.Add("(Enter custom name)");

            var x = choices.ToArray();
            return x;
        }

        public static string AskPrompt(string prompt, string value = null, bool useEditBox = false)
        {
            Console.Write(prompt);

            if (useEditBox)
            {
                var normal = new Colors(ConsoleColor.White, ConsoleColor.Blue);
                var text = EditBoxQuickEdit.Edit(40, 1, normal, value, 128);
                Console.WriteLine(text);
                return text;
            }

            if (!string.IsNullOrEmpty(value))
            {
                Console.WriteLine(value);
                return value;
            }

            return Console.ReadLine();
        }

        private static string DemandAskPrompt(string prompt, string value = null, bool useEditBox = false)
        {
            var answer = AskPrompt(prompt, value, useEditBox);
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
