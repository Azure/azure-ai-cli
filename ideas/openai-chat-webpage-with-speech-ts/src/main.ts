var echoOnly: boolean = false;
var continuousReco: boolean = true;

import { OpenAI} from 'openai';
import { OpenAIChatCompletionStreamingClass } from "./OpenAIChatCompletionStreamingClass";

// What's the system prompt?
const AZURE_OPENAI_SYSTEM_PROMPT = import.meta.env.AZURE_OPENAI_SYSTEM_PROMPT ?? "You are a helpful AI assistant.";

// NOTE: Never deploy your API Key in client-side environments like browsers or mobile apps
// SEE: https://help.openai.com/en/articles/5112595-best-practices-for-api-key-safety

// Get the required environment variables, and form the base URL for Azure OpenAI Chat Completions API
const AZURE_AI_SPEECH_KEY = import.meta.env.AZURE_AI_SPEECH_KEY;
const AZURE_AI_SPEECH_REGION = import.meta.env.AZURE_AI_SPEECH_REGION;
const AZURE_OPENAI_API_KEY = import.meta.env.AZURE_OPENAI_API_KEY ?? "<insert your Azure OpenAI API key here>";
const AZURE_OPENAI_API_VERSION = import.meta.env.AZURE_OPENAI_API_VERSION ?? "<insert your Azure OpenAI API version here>";
const AZURE_OPENAI_CHAT_DEPLOYMENT = import.meta.env.AZURE_OPENAI_CHAT_DEPLOYMENT ?? "<insert your Azure OpenAI chat deployment name here>";
const AZURE_OPENAI_ENDPOINT = import.meta.env.AZURE_OPENAI_ENDPOINT ?? "<insert your Azure OpenAI endpoint here>";
const AZURE_OPENAI_BASE_URL = `${AZURE_OPENAI_ENDPOINT.replace(/\/+$/, '')}/openai/deployments/${AZURE_OPENAI_CHAT_DEPLOYMENT}`;

// Check if the required environment variables are set
const ok = 
AZURE_OPENAI_SYSTEM_PROMPT != null && !AZURE_OPENAI_SYSTEM_PROMPT.startsWith('<insert') &&
AZURE_OPENAI_API_KEY != null && !AZURE_OPENAI_API_KEY.startsWith('<insert') &&
AZURE_OPENAI_API_VERSION != null && !AZURE_OPENAI_API_VERSION.startsWith('<insert') &&
AZURE_OPENAI_CHAT_DEPLOYMENT != null && !AZURE_OPENAI_CHAT_DEPLOYMENT.startsWith('<insert') &&
AZURE_OPENAI_ENDPOINT != null && !AZURE_OPENAI_ENDPOINT.startsWith('<insert');

if (!ok) {
    console.error('To use Azure OpenAI, set the following environment variables:\n' +
        '\n  AZURE_OPENAI_SYSTEM_PROMPT' +
        '\n  AZURE_OPENAI_API_KEY' +
        '\n  AZURE_OPENAI_API_VERSION' +
        '\n  AZURE_OPENAI_CHAT_DEPLOYMENT' +
        '\n  AZURE_OPENAI_ENDPOINT');
    console.error('\nYou can easily do that using the Azure AI CLI by doing one of the following:\n' +
        '\n  ai init' +
        '\n  ai dev shell' +
        '\n  node main.js' +
        '\n' +
        '\n  or' +
        '\n' +
        '\n  ai init' +
        '\n  ai dev shell --run "node main.js"');
    throw new Error('Missing environment variables');
}

// Create the OpenAI client
console.log('Using Azure OpenAI (w/ API Key)...');
const openai = new OpenAI({
    apiKey: AZURE_OPENAI_API_KEY,
    baseURL: AZURE_OPENAI_BASE_URL,
    defaultQuery: { 'api-version': AZURE_OPENAI_API_VERSION },
    defaultHeaders: { 'api-key': AZURE_OPENAI_API_KEY },
    dangerouslyAllowBrowser: true
});

// Create the streaming chat completions helper
const chat = new OpenAIChatCompletionStreamingClass(AZURE_OPENAI_CHAT_DEPLOYMENT, AZURE_OPENAI_SYSTEM_PROMPT, openai);

// Setup the speech parts
import * as AzureSpeech from 'microsoft-cognitiveservices-speech-sdk';
const speechConfig = AzureSpeech.SpeechConfig.fromSubscription(AZURE_AI_SPEECH_KEY, AZURE_AI_SPEECH_REGION);
const audioConfigMicrophone = AzureSpeech.AudioConfig.fromDefaultMicrophoneInput();
const recognizer = new AzureSpeech.SpeechRecognizer(speechConfig, audioConfigMicrophone);

var player: AzureSpeech.SpeakerAudioDestination | null = null;
var speakers: AzureSpeech.AudioConfig | null = null;
var synthesizer: AzureSpeech.SpeechSynthesizer | null = null;

function initSpeechSynthesizer(): AzureSpeech.SpeechSynthesizer {
    player = new AzureSpeech.SpeakerAudioDestination();
    speakers = AzureSpeech.AudioConfig.fromSpeakerOutput(player);
    synthesizer = new AzureSpeech.SpeechSynthesizer(speechConfig, speakers);
    return synthesizer;
}

function stopSpeaking(): void {
    if (player != null && synthesizer != null) {
        console.log('Stopping speech synthesis...');
        var p = player;
        var s = synthesizer;

        player = null;
        speakers = null;
        synthesizer = null;

        p.mute();
        s.close();
        p.close();
        console.log('Speech synthesis stopped.')
    }
}

function speak(text: string): void {
    if (synthesizer == null) {
        synthesizer = initSpeechSynthesizer();
    }
    synthesizer.speakTextAsync(
        text,
        result => {
            if (result.reason === AzureSpeech.ResultReason.SynthesizingAudioCompleted) {
                console.log('Speech synthesis completed.');
            } else {
                console.error('Speech synthesis failed: ', result.errorDetails);
            }
        },
        error => {
            console.error('Speech synthesis error: ', error);
        }
    );
}

var listening: boolean = false;
function toggleSpeechRecognition(): void{
    if (!listening) {
        console.log("Speech recognition started");
        if (continuousReco) {
            recognizer.startContinuousRecognitionAsync();
        }
        else {
            recognizer.recognizeOnceAsync();
        }
        listening = true;

        const textBox = document.getElementById('text-box') as HTMLInputElement;
        textBox.placeholder = 'Listening... (press ENTER to stop)';
    }
    else {
        console.log("Speech recognition stopping");
        recognizer.stopContinuousRecognitionAsync();
        listening = false;

        const textBox = document.getElementById('text-box') as HTMLInputElement;
        textBox.placeholder = 'Type here or press ENTER to start listening...';
    }
}

recognizer.recognizing = (_, e) => handleRecognizing(e);
recognizer.recognized = (_, e) => handleRecognized(e);

async function processUserInput(text: string): Promise<void> {
    var undefined: boolean = text == null || text == '' || text == "undefined";
    if (undefined) return;

    if (echoOnly) {
        console.log('User input: ' + text);
        speak(text);
        return;
    }

    var response = await chat.getResponse(text, (response: string) => {
        console.log('Chat response: ' + response);
    });
    speak(response);
}

function handleRecognizing(e: any) {
    console.log(`RECOGNIZING: Text=${e.result.text}`);
    stopSpeaking();

    const textBox = document.getElementById('text-box') as HTMLInputElement;
    textBox.value = e.result.text;
}

async function handleRecognized(e: any) {

    if (!continuousReco) {
        listening = false;
        updateButtonIcon();
    }

    if (e.result.reason === AzureSpeech.ResultReason.RecognizedSpeech) {
        console.log(`RECOGNIZED: Text=${e.result.text}`);
    } else if (e.result.reason === AzureSpeech.ResultReason.NoMatch) {
        console.log("NOMATCH: Speech could not be recognized.");
    }

    stopSpeaking();

    const textBox = document.getElementById('text-box') as HTMLInputElement;
    textBox.value = e.result.text;

    await processUserInput(textBox.value);
    textBox.value = '';

    updateButtonIcon();
}

function handleButtonClick(): void {
    const textBox = document.getElementById('text-box') as HTMLInputElement;
    if (textBox.value === '' || listening) {
        toggleSpeechRecognition();
        updateButtonIcon();
    } else {
        processUserInput(textBox.value);
        textBox.value = '';
        updateButtonIcon();
    }
}

function updateButtonIcon(): void {
    const textBox = document.getElementById('text-box') as HTMLInputElement;
    const button = document.getElementById('submit-button') as HTMLButtonElement;
    if (textBox.value === '') {
        if (listening) {
            button.innerHTML = '<i class="fa fa-solid fa-stop"></i>';
        }
        else {
            button.innerHTML = '<i class="fa fa-microphone" aria-hidden="true"></i>';
            textBox.placeholder = 'Type here or press ENTER to start listening...';
        }
    } else {
        button.innerHTML = '<i class="fa fa-solid fa-arrow-up"></i>';

    }
}

document.addEventListener('DOMContentLoaded', () => {

    updateButtonIcon();

    const button = document.getElementById('submit-button') as HTMLButtonElement;
    button.addEventListener('click', handleButtonClick);

    const textBox = document.getElementById('text-box') as HTMLInputElement;
    textBox.addEventListener('input', updateButtonIcon);
    textBox.addEventListener('keypress', (event) => {
        if (event.key === 'Enter') {
            handleButtonClick();
        }
    });

    textBox.focus();
});

