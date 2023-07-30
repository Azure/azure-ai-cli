//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System.Collections.Generic;

namespace Azure.AI.Details.Common.CLI
{
    public static class ScenarioActions
    {
        public static IList<ScenarioAction> Actions
        {
            get
            {
                return EnsureLoadActions();
            }
        }

        private static IList<ScenarioAction> EnsureLoadActions()
        {
            if (_tasks == null)
            {
                _tasks = new ScenarioAction[]
                {
                    new("Chat (OpenAI)", "Quickstart", null), // ChatOpenAiQuickStart),
                    new("Chat (OpenAI)", "or STEP 1:", "Initialize", null), // ChatOpenAiStep1Initialize),
                    new("Chat (OpenAI)", "   STEP 2:", "Interact/chat", null), // ChatOpenAiStep2Interact),
                    new("Chat (OpenAI)", "   STEP 3:", "Generate code", null), // ChatOpenAiStep3GenerateCode),
                    new("Chat (OpenAI)", "   STEP 4:", "Deploy", null), // ChatOpenAiStep4Deploy),
                    new("Chat (OpenAI)", "   STEP 5:", "Evaluate", null), // ChatOpenAiStep5Evaluate),
                    new("Chat (OpenAI)", "   STEP 6:", "Update", null), // ChatOpenAiStep6Update),
                    new("Chat (OpenAI)", "   STEP 7:", "Clean up", null), // ChatOpenAiStep7CleanUp),

                    new("Chat w/ your prompt (OpenAI)", "Quickstart", null), // ChatWithYourPromptOpenAiQuickStart),
                    new("Chat w/ your prompt (OpenAI)", "or STEP 1:", "Initialize", null), // ChatWithYourPromptOpenAiStep1Initialize),
                    new("Chat w/ your prompt (OpenAI)", "   STEP 2:", "Interact/chat", null), // ChatWithYourPromptOpenAiStep2Interact),
                    new("Chat w/ your prompt (OpenAI)", "   STEP 3:", "Generate code", null), // ChatWithYourPromptOpenAiStep3GenerateCode),
                    new("Chat w/ your prompt (OpenAI)", "   STEP 4:", "Deploy", null), // ChatWithYourPromptOpenAiStep4Deploy),
                    new("Chat w/ your prompt (OpenAI)", "   STEP 5:", "Evaluate", null), // ChatWithYourPromptOpenAiStep5Evaluate),
                    new("Chat w/ your prompt (OpenAI)", "   STEP 6:", "Update", null), // ChatWithYourPromptOpenAiStep6Update),
                    new("Chat w/ your prompt (OpenAI)", "   STEP 7:", "Clean up", null), // ChatWithYourPromptOpenAiStep7CleanUp),

                    new("Chat w/ your data (OpenAI)", "Quickstart", null), // ChatWithYourDataOpenAiQuickStart),
                    new("Chat w/ your data (OpenAI)", "or STEP 1:", "Initialize", null), // ChatWithYourDataOpenAiStep1Initialize),
                    new("Chat w/ your data (OpenAI)", "   STEP 2:", "Interact/chat", null), // ChatWithYourDataOpenAiStep2Interact),
                    new("Chat w/ your data (OpenAI)", "   STEP 3:", "Generate code", null), // ChatWithYourDataOpenAiStep3GenerateCode),
                    new("Chat w/ your data (OpenAI)", "   STEP 4:", "Deploy", null), // ChatWithYourDataOpenAiStep4Deploy),
                    new("Chat w/ your data (OpenAI)", "   STEP 5:", "Evaluate", null), // ChatWithYourDataOpenAiStep5Evaluate),
                    new("Chat w/ your data (OpenAI)", "   STEP 6:", "Update", null), // ChatWithYourDataOpenAiStep6Update),
                    new("Chat w/ your data (OpenAI)", "   STEP 7:", "Clean up", null), // ChatWithYourDataOpenAiStep7CleanUp),

                    new("Caption audio (Speech to Text)", "Quickstart", null), // CaptionAudioSpeechToTextQuickStart),
                    new("Caption audio (Speech to Text)", "or STEP 1:", "Initialize", null), // CaptionAudioSpeechToTextStep1Initialize),
                    new("Caption audio (Speech to Text)", "   STEP 2:", "Interact/caption audio", null), // CaptionAudioSpeechToTextStep2RecognizeSpeech),
                    new("Caption audio (Speech to Text)", "   STEP 3:", "Generate code", null), // CaptionAudioSpeechToTextStep3GenerateCode),
                    new("Caption audio (Speech to Text)", "   STEP 4:", "Deploy", null), // CaptionAudioSpeechToTextStep4Deploy),
                    new("Caption audio (Speech to Text)", "   STEP 5:", "Evaluate", null), // CaptionAudioSpeechToTextStep5Evaluate),
                    new("Caption audio (Speech to Text)", "   STEP 6:", "Update", null), // CaptionAudioSpeechToTextStep6Update),
                    new("Caption audio (Speech to Text)", "   STEP 7:", "Clean up", null), // CaptionAudioSpeechToTextStep7CleanUp),

                    new("Caption images and video (Vision)", "Quickstart", null), // CaptionImagesAndVideoVisionQuickStart),
                    new("Caption images and video (Vision)", "or STEP 1:", "Initialize", null), // CaptionImagesAndVideoVisionStep1Initialize),
                    new("Caption images and video (Vision)", "   STEP 2:", "Interact/caption", null), // CaptionImagesAndVideoVisionStep2RecognizeText),
                    new("Caption images and video (Vision)", "   STEP 3:", "Generate code", null), // CaptionImagesAndVideoVisionStep3GenerateCode),
                    new("Caption images and video (Vision)", "   STEP 4:", "Deploy", null), // CaptionImagesAndVideoVisionStep4Deploy),
                    new("Caption images and video (Vision)", "   STEP 5:", "Evaluate", null), // CaptionImagesAndVideoVisionStep5Evaluate),
                    new("Caption images and video (Vision)", "   STEP 6:", "Update", null), // CaptionImagesAndVideoVisionStep6Update),
                    new("Caption images and video (Vision)", "   STEP 7:", "Clean up", null), // CaptionImagesAndVideoVisionStep7CleanUp),

                    new("Extract text from images (Vision)", "Quickstart", null), // ExtractTextFromImagesVisionQuickStart),
                    new("Extract text from images (Vision)", "or STEP 1:", "Initialize", null), // ExtractTextFromImagesVisionStep1Initialize),
                    new("Extract text from images (Vision)", "   STEP 2:", "Interact/extract", null), // ExtractTextFromImagesVisionStep2ExtractText),
                    new("Extract text from images (Vision)", "   STEP 3:", "Generate code", null), // ExtractTextFromImagesVisionStep3GenerateCode),
                    new("Extract text from images (Vision)", "   STEP 4:", "Deploy", null), // ExtractTextFromImagesVisionStep4Deploy),
                    new("Extract text from images (Vision)", "   STEP 5:", "Evaluate", null), // ExtractTextFromImagesVisionStep5Evaluate),
                    new("Extract text from images (Vision)", "   STEP 6:", "Update", null), // ExtractTextFromImagesVisionStep6Update),
                    new("Extract text from images (Vision)", "   STEP 7:", "Clean up", null), // ExtractTextFromImagesVisionStep7CleanUp),

                    new("Extract text from documents and forms (Language)", "Quickstart", null), // ExtractTextFromDocumentsAndFormsLanguageQuickStart),
                    new("Extract text from documents and forms (Language)", "or STEP 1:", "Initialize", null), // ExtractTextFromDocumentsAndFormsLanguageStep1Initialize),
                    new("Extract text from documents and forms (Language)", "   STEP 2:", "Interact/extract", null), // ExtractTextFromDocumentsAndFormsLanguageStep2ExtractText),
                    new("Extract text from documents and forms (Language)", "   STEP 3:", "Generate code", null), // ExtractTextFromDocumentsAndFormsLanguageStep3GenerateCode),
                    new("Extract text from documents and forms (Language)", "   STEP 4:", "Deploy", null), // ExtractTextFromDocumentsAndFormsLanguageStep4Deploy),
                    new("Extract text from documents and forms (Language)", "   STEP 5:", "Evaluate", null), // ExtractTextFromDocumentsAndFormsLanguageStep5Evaluate),
                    new("Extract text from documents and forms (Language)", "   STEP 6:", "Update", null), // ExtractTextFromDocumentsAndFormsLanguageStep6Update),
                    new("Extract text from documents and forms (Language)", "   STEP 7:", "Clean up", null), // ExtractTextFromDocumentsAndFormsLanguageStep7CleanUp),

                    new("Transcribe and analyze calls (Speech, Language)", "Quickstart", null), // TranscribeAndAnalyzeCallsSpeechLanguageQuickStart),
                    new("Transcribe and analyze calls (Speech, Language)", "or STEP 1:", "Initialize", null), // TranscribeAndAnalyzeCallsSpeechLanguageStep1Initialize),
                    new("Transcribe and analyze calls (Speech, Language)", "   STEP 2:", "Interact/transcribe", null), // TranscribeAndAnalyzeCallsSpeechLanguageStep2TranscribeCalls),
                    new("Transcribe and analyze calls (Speech, Language)", "   STEP 3:", "Generate code", null), // TranscribeAndAnalyzeCallsSpeechLanguageStep3GenerateCode),
                    new("Transcribe and analyze calls (Speech, Language)", "   STEP 4:", "Deploy", null), // TranscribeAndAnalyzeCallsSpeechLanguageStep4Deploy),
                    new("Transcribe and analyze calls (Speech, Language)", "   STEP 5:", "Evaluate", null), // TranscribeAndAnalyzeCallsSpeechLanguageStep5Evaluate),
                    new("Transcribe and analyze calls (Speech, Language)", "   STEP 6:", "Update", null), // TranscribeAndAnalyzeCallsSpeechLanguageStep6Update),
                    new("Transcribe and analyze calls (Speech, Language)", "   STEP 7:", "Clean up", null), // TranscribeAndAnalyzeCallsSpeechLanguageStep7CleanUp),

                    new("Translate documents and text (Language)", "Quickstart", null), // TranslateDocumentsAndTextLanguageQuickStart),
                    new("Translate documents and text (Language)", "or STEP 1:", "Initialize", null), // TranslateDocumentsAndTextLanguageStep1Initialize),
                    new("Translate documents and text (Language)", "   STEP 2:", "Interact/translate", null), // TranslateDocumentsAndTextLanguageStep2TranslateText),
                    new("Translate documents and text (Language)", "   STEP 3:", "Generate code", null), // TranslateDocumentsAndTextLanguageStep3GenerateCode),
                    new("Translate documents and text (Language)", "   STEP 4:", "Deploy", null), // TranslateDocumentsAndTextLanguageStep4Deploy),
                    new("Translate documents and text (Language)", "   STEP 5:", "Evaluate", null), // TranslateDocumentsAndTextLanguageStep5Evaluate),
                    new("Translate documents and text (Language)", "   STEP 6:", "Update", null), // TranslateDocumentsAndTextLanguageStep6Update),
                    new("Translate documents and text (Language)", "   STEP 7:", "Clean up", null), // TranslateDocumentsAndTextLanguageStep7CleanUp),

                    new("Summarize documents (Language)", "Quickstart", null), // SummarizeDocumentsLanguageQuickStart),
                    new("Summarize documents (Language)", "or STEP 1:", "Initialize", null), // SummarizeDocumentsLanguageStep1Initialize),
                    new("Summarize documents (Language)", "   STEP 2:", "Interact/summarize", null), // SummarizeDocumentsLanguageStep2SummarizeDocuments),
                    new("Summarize documents (Language)", "   STEP 3:", "Generate code", null), // SummarizeDocumentsLanguageStep3GenerateCode),
                    new("Summarize documents (Language)", "   STEP 4:", "Deploy", null), // SummarizeDocumentsLanguageStep4Deploy),
                    new("Summarize documents (Language)", "   STEP 5:", "Evaluate", null), // SummarizeDocumentsLanguageStep5Evaluate),
                    new("Summarize documents (Language)", "   STEP 6:", "Update", null), // SummarizeDocumentsLanguageStep6Update),
                    new("Summarize documents (Language)", "   STEP 7:", "Clean up", null), // SummarizeDocumentsLanguageStep7CleanUp),

                    new("...more!", "", null), // More),
                };
            }
            return _tasks;
        }

        private static IList<ScenarioAction> _tasks = null;
    }
}
