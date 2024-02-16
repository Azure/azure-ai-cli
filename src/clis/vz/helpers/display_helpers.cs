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
using System.Drawing;
using Azure.AI.Vision.ImageAnalysis;

namespace Azure.AI.Details.Common.CLI
{
    public class DisplayHelper
    {
        public DisplayHelper(ICommandValues values)
        {
            _values = values;
            _quiet = _values.GetOrDefault("x.quiet", false);
            _verbose = values.GetOrDefault("x.verbose", false);
        }

        // public void DisplayConnected(ConnectionEventArgs e)
        // {
        //     if (_quiet) return;
        //     Console.WriteLine("Connection CONNECTED...");
        // }

        // public void DisplayDisconnected(ConnectionEventArgs e)
        // {
        //     if (_quiet) return;
        //     Console.WriteLine("Connection DISCONNECTED.");
        // }

        // public void DisplayMessageReceived(ConnectionMessageEventArgs e)
        // {
        //     if (_quiet || !_verbose) return;
        //     Console.WriteLine("MESSAGE RECEIVED: {0}", e.Message.Path);
        // }

        // public void DisplaySessionStarted(SessionEventArgs e)
        // {
        //     if (_quiet) return;
        //     Console.WriteLine("SESSION STARTED: {0}\n", e.SessionId);
        // }

        // public void DisplaySessionStopped(SessionEventArgs e)
        // {
        //     if (_quiet) return;
        //     Console.WriteLine("SESSION STOPPED: {0}", e.SessionId);
        // }

        public void DisplayAnalyzed(ImageAnalysisEventArgs e)
        {
            if (_quiet) return;

            var result = e.Result;
            if (result.Reason == ImageAnalysisResultReason.Analyzed)
            {
                Console.WriteLine($"ANALYZED:\n\n{GetDisplayText(result)}");
                Console.WriteLine();
            }
        }

        private string GetDisplayText(ImageAnalysisResult result)
        {
            var kvp1 = new KeyValuePair<string, string>[]{
                // Each one of these results will (possibly) be displayed in multiple lines
                GetCaptionDisplayText(result),
                GetDetectedPeopleDisplayText(result),
                GetDetectedObjectsDisplayText(result),
                GetTagsDisplayText(result),
                GetDetectedTextDisplayText(result),
            };
            var kvp2 = new KeyValuePair<string, string>[]{
                // Each one of these results will be displayed on a single line
                GetFileDisplayText(result, _values),
                GetSizeDisplayText(result),
                GetModelVersionDisplayText(result),
                GetCropDisplayText(result)
            };

            var k0 = "CROP_SUGGESTIONS".Length; // The key with the longest name. Needed for proper alignment in output display
            var k1 = kvp1.Max(x => x.Key.Length);
            var k2 = kvp2.Max(x => x.Key.Length);
            var maxK = Math.Max(Math.Max(k0, k1), k2);

            var part1 = GetDisplayText(2, maxK, kvp1);
            var part2 = GetDisplayText(2, maxK, kvp2);

            var part1Ok = !string.IsNullOrWhiteSpace(part1);
            var part2Ok = !string.IsNullOrWhiteSpace(part2);
            return part1Ok && part2Ok
                ? $"{part1}\n\n{part2}"
                : part1Ok ? part1 : part2;
        }

        private static string GetDisplayText(int indent, int keyWidth, IEnumerable<KeyValuePair<string, string>> things)
        {
            var pad2 = new string(' ', indent + keyWidth + 2);

            var sb = new StringBuilder();
            foreach (var item in things)
            {
                if (string.IsNullOrWhiteSpace(item.Key)) continue;
                if (string.IsNullOrWhiteSpace(item.Value)) continue;

                var pad1 = new string(' ', keyWidth - item.Key.Length + indent);
                sb.Append(pad1);
                sb.Append($"{item.Key}: ");

                var skipPad2 = true;
                foreach (var line in item.Value.Split('\n'))
                {
                    if (!skipPad2) sb.Append(pad2);
                    if (skipPad2) skipPad2 = false;
                    sb.AppendLine(line);
                }
            }

            return sb.ToString().Trim('\r', '\n');
        }

        private static string ReplaceMultilineSpacingWithSingleSpacing(string text)
        {
            while (text.Contains("\n\n"))
            {
                text = text.Replace("\n\n", "\n");
            }
            while (text.Contains("\r\n\r\n"))
            {
                text = text.Replace("\r\n\r\n", "\r\n");
            }

            return text;
        }

        private static string ReplaceMultilineSpacingWithDoubleSpacing(string text)
        {
            while (text.Contains("\n\n\n"))
            {
                text = text.Replace("\n\n\n", "\n\n");
            }
            while (text.Contains("\r\n\r\n\r\n"))
            {
                text = text.Replace("\r\n\r\n\r\n", "\r\n\r\n");
            }

            return text;
        }

        private static KeyValuePair<string, string> GetFileDisplayText(ImageAnalysisResult result, ICommandValues values)
        {
            var fileName = values.GetOrDefault("vision.input.id", "(file name not found)");
            return NewPair("IMAGE_ID", fileName);
        }

        private static KeyValuePair<string, string> GetModelVersionDisplayText(ImageAnalysisResult result)
        {
            return NewPair("MODEL_VERSION", result.ModelVersion);
        }

        private static KeyValuePair<string, string> GetSizeDisplayText(ImageAnalysisResult result)
        {
            return NewPair("IMAGE_SIZE", $"{result.ImageWidth}x{result.ImageHeight}");
        }

        private static KeyValuePair<string, string> GetCropDisplayText(ImageAnalysisResult result)
        {
            var count = result.CropSuggestions?.Count();
            var cropsWithRect = result.CropSuggestions?.Select(x => $"{GetRectDisplayText(x.BoundingBox)} (aspect ratio {x.AspectRatio})");

            var firstCropAsString = count > 0 ? cropsWithRect.First() : "";
            return NewPair("CROP_SUGGESTIONS", firstCropAsString);
        }

        private static KeyValuePair<string, string> GetTagsDisplayText(ImageAnalysisResult result)
        {
            var count = result.Tags?.Count();
            var names = result.Tags?.Select(x => x.Name);

            var tags = count > 0 ? string.Join(", ", names) : "";
            return NewPair($"{count} TAG(s)", tags);
        }

        private static KeyValuePair<string, string> GetDetectedPeopleDisplayText(ImageAnalysisResult result)
        {
            var count = result.People?.Count();
            var peopleWithRect = result.People?.Select(x => $"person ({GetRectDisplayText(x.BoundingBox)})");

            var peopleWithRectAsString = count > 0 ? string.Join("\n", peopleWithRect) : "";
            return NewPair($"{count} PERSON(s)", peopleWithRectAsString);
        }

        private static KeyValuePair<string, string> GetDetectedObjectsDisplayText(ImageAnalysisResult result)
        {
            var count = result.Objects?.Count();
            var objectsWithRect = result.Objects?.Select(x => $"{x.Name} ({GetRectDisplayText(x.BoundingBox)})");
            
            var objectsWithRectAsString = count > 0 ? string.Join("\n", objectsWithRect) : "";
            return NewPair($"{count} OBJECT(s)", objectsWithRectAsString);
        }

        private static KeyValuePair<string, string> GetDetectedTextDisplayText(ImageAnalysisResult result)
        {
            return NewPair("TEXT", string.Join('\n', result.Text?.Lines.Select(line => line.Content)));
        }

        private static KeyValuePair<string, string> GetCaptionDisplayText(ImageAnalysisResult result)
        {
            string caption = result.Caption?.Content;
            return NewPair("CAPTION", caption);
        }

        private static string GetRectDisplayText(Rectangle rect)
        {
            return $"{rect.Width}x{rect.Height} at {rect.X},{rect.Y}";
        }

        private static KeyValuePair<string, string> NewPair(string key, string value)
        {
            return !string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value)
                ? new KeyValuePair<string, string>(key, value)
                : new KeyValuePair<string, string>("", "");
        }

        private static string NotNullPrependOrNull(string prepend, string item)
        {
            return string.IsNullOrWhiteSpace(item) ? null : $"{prepend}{item}";
        }

        private ICommandValues _values;
        private bool _quiet;
        private bool _verbose;
    }
}
