<#@ template hostspecific="true" #>
<#@ output extension=".py" encoding="utf-8" #>
<#@ parameter type="System.String" name="AZURE_AI_SPEECH_KEY" #>
<#@ parameter type="System.String" name="AZURE_AI_SPEECH_REGION" #>
from concurrent.futures import Future
from azure.cognitiveservices.speech import SpeechConfig, SpeechRecognizer, AudioConfig, CancellationReason
import threading
import os

# Connection and configuration details required
speech_key = os.environ.get('AZURE_AI_SPEECH_KEY') or "<#= AZURE_AI_SPEECH_KEY #>"
service_region = os.environ.get('AZURE_AI_SPEECH_REGION') or "<#= AZURE_AI_SPEECH_REGION #>"
speech_language = "en-US"

# Create instances of a speech config and audio config
speech_config = SpeechConfig(subscription=speech_key, region=service_region, speech_recognition_language=speech_language)
audio_config = AudioConfig(use_default_microphone=True)

# Create the speech recognizer from the above configuration information
speech_recognizer = SpeechRecognizer(speech_config=speech_config, audio_config=audio_config)

# Start speech recognition, and return after a single utterance is recognized. The end of a
# single utterance is determined by listening for silence at the end or until a maximum of 15
# seconds of audio is processed.
print("Listening ...\n")
result = speech_recognizer.recognize_once()

# Check the result
if result.reason.name == "RecognizedSpeech" and result.text != "":
    print("RECOGNIZED: {}".format(result.text))
elif result.reason.name == "NoMatch":
    print("NOMATCH: Speech could not be recognized.")
elif result.reason.name == "Canceled":
    cancellation_details = result.cancellation_details
    print("CANCELED: Reason={}".format(cancellation_details.reason))
    if cancellation_details.reason == CancellationReason.Error:
        print("CANCELED: ErrorDetails={}".format(cancellation_details.error_details))
        print("CANCELED: Did you update the subscription info?")
