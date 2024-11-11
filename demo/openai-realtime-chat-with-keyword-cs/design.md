Here's a design that I'd like your help to build using C#.

## AudioSourceController Design

The `AudioSourceController` class will be used as part of a larger voice input/output conversational system. This overall system will allow the user to interact via their voice and will talk back to the user based on what the user says and what the system can do. The `AudioSourceController` will manage the audio sources and transitions between them. The system will have three primary states: "Off", "Open Mic", and "Keyword Armed". The transitions between these states will be controlled by the user through a keyboard interface. 

The `AudioSourceController` will have methods that external classes will call to transition between states. The `AudioSourceController` will also have events that external classes can subscribe to be notified of state changes, data availability, and audio and display outputs. 

There will also be a global mute that will mute all audio output from the system. This will be controlled by an external class that will call a method on the `AudioSourceController` to mute or unmute the system. This will be especially useful when the system is talking back to the user on systems that do not have integrated echo cancellation hardware or software.

## Externally visible primary states
- Off (no audio flows)
- Open Mic (audio flowing)
- Keyword armed (kws is armed, but no audio flowing "out" of the system)

## Transitions
| State | New State | Trigger | Visual | Sound |
|-------|-----------|---------|--------|-------|
| Off | Open Mic | &lt;Space&gt; | Listening: ... | high beep |
| Off | Keyword armed | &lt;Space&gt; (1s) | Sleeping ... | two low beeps |
| Open Mic | Off | &lt;Space&gt; | Off | low beep |
| Open Mic | Keyword armed | &lt;Space&gt; (1s)  | Sleeping ... | two low beeps |
| Keyword armed | Open Mic | &lt;Space&gt; | Listening: ... | high beep |
| Keyword armed | Off | &lt;Space&gt; (1s) | Off | low beep |
| Keyword armed | Open Mic | "Keyword" | Listening: keyword ... | none |

## Open Mic Mode
Open Mic "mode" may source audio from either:
- Keyword recognizer (16khz 16bit mono) -> Resampler (16khz -> 24khz)
- NAudio (24khz 16bit mono)

## External Mute
Since we're integrating with other components that will "speak" back to the user, we must also provide the ability to "mute" the system.

This will be a global mute, and will not affect the internal state of the system.

## Classes

- `AudioSourceController` - Manages all audio sources and transitions between them
- `AudioResampler16KhzTo24Khz` - Resamples 16khz audio to 24khz audio
- `KeywordGatedAudio16khzSource` - Manages Azure Cognitive Services Keyword Recognizer
- `KeywordGatedAudio24KhzSource` - Uses `KeywordGatedAudio16khzSource` and `AudioResampler16KhzTo24Khz` to provide 24khz audio
- `NAudioMicrophone24KhzSource` - Manages NAudio capture of audio

