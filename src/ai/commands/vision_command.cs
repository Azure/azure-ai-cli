//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Azure.AI.Vision.ImageAnalysis;

namespace Azure.AI.Details.Common.CLI
{
    public class VisionCommand : Command
    {
        internal VisionCommand(ICommandValues values)
        {
            _values = values.ReplaceValues();
            _quiet = _values.GetOrDefault("x.quiet", false);
            _verbose = _values.GetOrDefault("x.verbose", true);
        }

        internal bool RunCommand()
        {
            try
            {
                RunVisionCommand();
            }
            catch (WebException ex)
            {
                ConsoleHelpers.WriteLineError($"\n  ERROR: {ex.Message}");
                JsonHelpers.PrintJson(HttpHelpers.ReadWriteJson(ex.Response, _values, "vision"));
            }

            return _values.GetOrDefault("passed", true);
        }

        private bool RunVisionCommand()
        {
            DoCommand(_values.GetCommand());
            return _values.GetOrDefault("passed", true);
        }

        private void DoCommand(string command)
        {
            CheckPath();

            switch (command)
            {
                case "vision.image.analyze":
                    RunImageAnalyzeCommand(_values);
                    break;
                default:
                    _values.AddThrowError("WARNING:", $"'{command.Replace('.', ' ')}' NOT YET IMPLEMENTED!!");
                    break;
            }
        }

        private void RunImageAnalyzeCommand(ICommandValues values)
        {
            var featuresString = _values["vision.image.visual.features"];
            var featuresSplit = featuresString.Split(',');
            VisualFeatures features = VisualFeatures.None;
            foreach (var feature in featuresSplit)
            {
                features |= (VisualFeatures)Enum.Parse(typeof(VisualFeatures), feature, true);
            }

            var client = new ImageAnalysisClient(new Uri(_values["service.config.endpoint.uri"]), new AzureKeyCredential(_values["service.config.key"]));

            var options = BuildIAOptions(values);

            ImageAnalysisResult result = null;
            switch (_values["vision.input.type"])
            {
                case "url":
                    var url = _values["vision.input.url"];
                    result = client.Analyze(new Uri(url), features, options);
                    break;
                case "file":
                    var path = _values["vision.input.file"];
                    result = client.Analyze(BinaryData.FromBytes(System.IO.File.ReadAllBytes(path)), features, options);
                    break;
            }
            
            var outputType = _values.GetOrDefault("vision.output.type", "text");
            switch(outputType)
            {
                case "text":
                    PrintIAResult(result, Console.Out);
                    break;
                case "json":
                    Console.Write(System.ClientModel.Primitives.ModelReaderWriter.Write(result).ToString());
                    break;
                default:
                    _values.AddThrowError("WARNING:", $"'{outputType}' NOT YET IMPLEMENTED!!");
                    break;
            }
        }

        private ImageAnalysisOptions BuildIAOptions(ICommandValues values)
        {
            var options = new ImageAnalysisOptions();
            if (values.Contains("vision.image.language"))
            {
                options.Language = values.Get("vision.image.language");
            }
            if(values.Contains("vision.image.gender.neutral.caption"))
            {
                options.GenderNeutralCaption = bool.Parse(values.Get("vision.image.gender.neutral.caption"));
            }
            if(values.Contains("vision.image.model.version"))
            {
                options.ModelVersion = values.Get("vision.image.model.version");
            }
            if(values.Contains("vision.image.smart.crop.aspect.ratio"))
            {
                List<float> aspectRatios = new List<float>();
                var ratios = values.Get("vision.image.smart.crop.aspect.ratio").Split(',');
                foreach(var ratio in ratios)
                {
                    aspectRatios.Add(float.Parse(ratio));
                }
                options.SmartCropsAspectRatios = aspectRatios;
            }
            return options;
        }

        private void PrintIAResult(ImageAnalysisResult result, TextWriter writer)
        {
            writer.WriteLine($"Image analysis results:");
            writer.WriteLine($" Metadata: Model: {result.ModelVersion} Image dimensions: {result.Metadata.Width} x {result.Metadata.Height}");

            if (result.Caption != null)
            {
                writer.WriteLine($" Caption: {result.Caption.Text}, Confidence: {result.Caption.Confidence:F4}");
            }
            if (result.DenseCaptions != null)
            {
                writer.WriteLine($" Dense Captions:");
                foreach (DenseCaption denseCaption in result.DenseCaptions.Values)
                {
                    writer.WriteLine($"   Region: '{denseCaption.Text}', Confidence: {denseCaption.Confidence:F4}, Bounding box: {denseCaption.BoundingBox}");
                }
            }
            if (result.Tags != null)
            {
                writer.WriteLine($" Tags:");
                foreach (DetectedTag tag in result.Tags.Values)
                {
                    writer.WriteLine($"   '{tag.Name}', Confidence: {tag.Confidence:F4}");
                }
            }
            if (result.Objects != null)
            {
                writer.WriteLine($" Objects:");
                foreach (DetectedObject detectedObject in result.Objects?.Values)
                {
                    writer.WriteLine($"   Object: '{detectedObject.Tags.First().Name}', Bounding box: {detectedObject.BoundingBox.ToString()}");
                }
            }
            if (result.SmartCrops != null)
            {
                writer.WriteLine($" SmartCrops:");
                foreach (CropRegion cropRegion in result.SmartCrops?.Values)
                {
                    writer.WriteLine($"   Aspect ratio: {cropRegion.AspectRatio}, Bounding box: {cropRegion.BoundingBox}");
                }
            }
            if (result.People != null)
            {
                writer.WriteLine($" People:");
                foreach (DetectedPerson person in result.People?.Values)
                {
                    writer.WriteLine($"   Person: Bounding box {person.BoundingBox.ToString()}, Confidence: {person.Confidence:F4}");
                }
            }
            if (result.Read != null)
            {
                writer.WriteLine($" Read:");
                foreach (var line in result.Read?.Blocks.SelectMany(block => block.Lines))
                {
                    writer.WriteLine($"   Line: '{line.Text}', Bounding Polygon: [{string.Join(" ", line.BoundingPolygon)}]");
                    foreach (DetectedTextWord word in line.Words)
                    {
                        writer.WriteLine($"     Word: '{word.Text}', Confidence {word.Confidence.ToString("#.####")}, Bounding Polygon: [{string.Join(" ", word.BoundingPolygon)}]");
                    }
                }
            }
        }
        private bool _quiet = false;
        private bool _verbose = false;
    }
}
