# How do I run SPX with embedded SR and TTS in private preview?

Updated 11/11/2021.

## Install Carbon packages

1. In Visual Studio, open menu *Tools* \> *NuGet Package Manager* \> *Manage NuGet packages for Solution...*
1. Use the **Spx-Upstream-Release** package source (default) and ensure that the option **Include prerelease** is checked.
1. Browse and install the latest internal pre-release versions of all the following packages:
     * `Microsoft.CognitiveServices.Speech`
     * `Microsoft.CognitiveServices.Speech.Extension.Embedded.SR`
     * `Microsoft.CognitiveServices.Speech.Extension.Embedded.TTS`
     * `Microsoft.CognitiveServices.Speech.Extension.ONNX.Runtime`

   **Note:** If there is an error "*Could not find a part of the path*" while trying to install/restore packages, it is because of the maximum length for a path in Windows. In that case move the spx folder to the disk drive root or close to it, so that folder and file paths become shorter.

## Get embedded models

### SR

See Carbon `external\sr_runtime\README.md` for instructions on downloading. It is enough to get only the model necessary for testing, like `FP\en-US\V6\onnx` (en-US).

### TTS

See Carbon `external\offline_tts\README.md` for instructions on downloading. It is enough to get only the model necessary for testing, like `Unencrypted\ArialNeural` (en-US).

## Usage

Use the same spx commands as with online, but add embedded specific switches:
* `--embedded` - Enable embedded in general (required).
* `--embeddedModelPath <PATH>` - Specify the path to the embedded model to use (required).
* `--embeddedModelKey <KEY>` - Specify the model decryption key (optional, not needed if the model is unencrypted).

Online specific parameters like service region and subscription key are not needed.

### Example, SR

```
spx.exe recognize --embedded --embeddedModelPath ..\sr-models\FP\en-US\V6\onnx [...]
```

### Example, TTS

```
spx.exe synthesize --embedded --embeddedModelPath ..\tts-voices\AriaNeural [...]
```