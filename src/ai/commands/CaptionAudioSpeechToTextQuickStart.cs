//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Buffers.Text;
using System.Reflection.Metadata;
using static System.Net.Mime.MediaTypeNames;
using System.Reflection.PortableExecutable;
using System.Xml.Linq;
using System.Transactions;

namespace Azure.AI.Details.Common.CLI
{
    internal class CaptionAudioSpeechToTextQuickStart
    {
        internal static void Run(ScenarioAction action)
        {
            ConsoleHelpers.WriteLineWithHighlight($"\n`{action.Scenario}: {action.Action}`".ToUpper());
            ConsoleHelpers.WriteLineWithHighlight("\nSee: `#e0;https://learn.microsoft.com/azure/ai-services/speech-service/captioning-concepts`");
            Console.WriteLine();
            Console.WriteLine("     In this guide, you learn how to create captions with speech to text.");
            Console.WriteLine("     Captioning is the process of converting the audio content of a television");
            Console.WriteLine("     broadcast, webcast, film, video, live event, or other production into");
            Console.WriteLine("     text, and then displaying the text on a screen, monitor, or other visual");
            Console.WriteLine("     display system.");
            Console.WriteLine();
        }
    }

    internal class CaptionImagesAndVideoVisionQuickStart
    {
        internal static void Run(ScenarioAction action)
        {
            ConsoleHelpers.WriteLineWithHighlight($"\n`{action.Scenario}: {action.Action}`".ToUpper());
            ConsoleHelpers.WriteLineWithHighlight("\nSee: `#e0;https://learn.microsoft.com/azure/ai-services/computer-vision/quickstarts-sdk/image-analysis-client-library-40`");
            Console.WriteLine();
            Console.WriteLine("     Get started with the Image Analysis 4.0 REST API or client SDK to set up");
            Console.WriteLine("     a basic image analysis application. The Image Analysis service provides");
            Console.WriteLine("     you with AI algorithms for processing images and returning information on");
            Console.WriteLine("     their visual features. Follow these steps to install a package to your");
            Console.WriteLine("     application and try out the sample code.");
            Console.WriteLine();
        }
    }

    internal class ExtractTextFromImagesVisionQuickStart
    {
        internal static void Run(ScenarioAction action)
        {
            ConsoleHelpers.WriteLineWithHighlight($"\n`{action.Scenario}: {action.Action}`".ToUpper());
            ConsoleHelpers.WriteLineWithHighlight("\nSee: `#e0;https://learn.microsoft.com/azure/ai-services/computer-vision/concept-ocr`");
            Console.WriteLine();
            Console.WriteLine("     The new Computer Vision Image Analysis 4.0 REST API offers the ability to");
            Console.WriteLine("     extract printed or handwritten text from images in a unified performance-");
            Console.WriteLine("     enhanced synchronous API that makes it easy to get all image insights");
            Console.WriteLine("     including OCR results in a single API operation.");
            Console.WriteLine();
        }
    }

    internal class ExtractTextFromDocumentsAndFormsLanguageQuickStart
    {
        internal static void Run(ScenarioAction action)
        {
            ConsoleHelpers.WriteLineWithHighlight($"\n`{action.Scenario}: {action.Action}`".ToUpper());
            ConsoleHelpers.WriteLineWithHighlight("\nSee: `#e0;https://learn.microsoft.com/azure/ai-services/document-intelligence/quickstarts/get-started-sdks-rest-api`");
            Console.WriteLine();
            Console.WriteLine("     Azure AI Document Intelligence is a cloud-based Azure AI service that uses");
            Console.WriteLine("     machine learning to extract key-value pairs, text, tables and key data from");
            Console.WriteLine("     your documents.You can easily integrate Document Intelligence models into");
            Console.WriteLine("     your workflows and applications by using an SDK in the programming language");
            Console.WriteLine("     of your choice or calling the REST API.");
            Console.WriteLine();
        }
    }

    internal class TranscribeAndAnalyzeCallsSpeechLanguageQuickStart
    {
        internal static void Run(ScenarioAction action)
        {
            ConsoleHelpers.WriteLineWithHighlight($"\n`{action.Scenario}: {action.Action}`".ToUpper());
            ConsoleHelpers.WriteLineWithHighlight("\nSee: `#e0;https://learn.microsoft.com/azure/ai-services/speech-service/call-center-quickstart`");
            Console.WriteLine();
            Console.WriteLine("     Azure AI services for Language and Speech can help you realize partial or");
            Console.WriteLine("     full automation of telephony-based customer interactions, and provide");
            Console.WriteLine("     accessibility across multiple channels. With the Language and Speech");
            Console.WriteLine("     services, you can further analyze call center transcriptions, extract and");
            Console.WriteLine("     redact conversation personally identifiable information (PII), summarize");
            Console.WriteLine("     the transcription, and detect the sentiment.");
            Console.WriteLine();
        }
    }

    internal class TranslateDocumentsAndTextLanguageQuickStart
    {
        internal static void Run(ScenarioAction action)
        {
            ConsoleHelpers.WriteLineWithHighlight($"\n`{action.Scenario}: {action.Action}`".ToUpper());
            ConsoleHelpers.WriteLineWithHighlight("\nSee: `#e0;https://learn.microsoft.com/azure/ai-services/translator/quickstart-text-sdk`");
            Console.WriteLine();
            Console.WriteLine("     Azure Text Translation is a cloud-based REST API feature of the Translator");
            Console.WriteLine("     service that uses neural machine translation technology to enable quick");
            Console.WriteLine("     and accurate source-to-target text translation in real time across all");
            Console.WriteLine("     supported languages.");
            Console.WriteLine();
        }
    }

    internal class SummarizeDocumentsLanguageQuickStart
    {
        internal static void Run(ScenarioAction action)
        {
            ConsoleHelpers.WriteLineWithHighlight($"\n`{action.Scenario}: {action.Action}`".ToUpper());
            ConsoleHelpers.WriteLineWithHighlight("\nSee: `#e0;https://learn.microsoft.com/azure/ai-services/language-service/summarization/quickstart`");
            Console.WriteLine();
            Console.WriteLine("     Summarization is one of the features offered by Azure AI Language, a");
            Console.WriteLine("     collection of machine learning and AI algorithms in the cloud for");
            Console.WriteLine("     developing intelligent applications that involve written language.Use");
            Console.WriteLine("     this article to learn more about this feature, and how to use it in");
            Console.WriteLine("     your applications.");
            Console.WriteLine();
        }
    }
}
