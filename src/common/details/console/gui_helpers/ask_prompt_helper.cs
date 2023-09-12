//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using Azure.AI.Details.Common.CLI.ConsoleGui;

namespace Azure.AI.Details.Common.CLI
{
    public class AskPromptHelper
    {
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
    }
}
