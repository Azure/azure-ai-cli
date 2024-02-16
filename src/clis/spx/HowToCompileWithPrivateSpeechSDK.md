# How to localy compile SPX with a private (internal) version of speech SDK?

Here we will assume the latest public version is 1.17, but you would like to compile against an internal 1.18 preview version (any internal Carbon build).

You can do that in two ways:

1. Build SPX with existing Visual Studio project that consumes the public Speech SDK 1.17 NuGet, and then before you run SPX from the console, replace Speech SDK 1.17 binaries with 1.18 preview binaries. Get preview binaries from latest Windows x64 release artifact of a green build of the Carbon master branch (["Yml - Carbon Build"](https://msasg.visualstudio.com/Skyman/_build?definitionId=4833&_a=summary&repositoryFilter=1005&branchFilter=257627%2C257627%2C257627%2C257627%2C257627%2C257627%2C257627%2C257627%2C257627%2C257627%2C257627) ADO pipeline).
   * Note the changed filenames since 1.17:
     * onnxruntime.dll -> Microsoft.CognitiveServices.Speech.extension.onnxruntime.dll
     * libMSTTSEngine.dll -> Microsoft.CognitiveServices.Speech.extension.embedded.tts.runtime.dll
1. Set up a local NuGet feed for the Speech SDK 1.18 preview. Update the SPX Visual Studio solution to use this local feed instead of the public 1.17 feed. Build and debug using Visual Studio as you would normally do.

If you ever tried to replace NuGet binaries in the output folder of a Visual Studio project, you've noticed that whenever you build the project or start debugging, Visual Studio automatically detects that NuGet binaries were changed, and it restores the original binaries. This is very frustrating. Visual Studio has two check boxes in the NuGet Package Manager (click on the gears icon, and then expand "NuGet Package Manager -> General"). They are checked by default and named:
- "Allow NuGet to download missing packages"
- "Automatically check for missing packages during build in Visual Studio"

Perhaps if you un-check the 2nd option, it will not try to refresh the Speech SDK binaries when you launch the debugger, allowing you to debug with preview Speech SDK binaries (if you select option #1). I (Darren) have not tried it and will update this document if/when I give it a try.

If you decide to go with option #2 (recommended), read the next section. 

### Installing a local NuGet feed

Do the following:

1. Pick an artifact from a successful build of carbon ([Yml - Carbon Build build pipeline](https://msasg.visualstudio.com/Skyman/_build?definitionId=4833&_a=summary)). In the Windows folder of the artifact, locate a file named similar to  this ```Microsoft.CognitiveServices.Speech.1.14.0-alpha.0.15922490.nupkg```. 
1. Download this .nupkg package to your PC (does not matter where, you will not need it for long). Let's say I downloaded it to ```c:\temp```
1. Got to https://www.nuget.org/downloads and download the recommended NuGet command line (NuGet.exe) under ```Windows x86 Commandline``` (this is not available as part of Visual Studio). Put it anywhere on your PC that's in the search PATH so you can execute it.
1. Create another folder on your PC that will serve as the local NuGet feed. Let's say this folder is called ```c:\local-nuget-feed```.
1. Run this command ```nuget.exe add c:\temp\Microsoft.CognitiveServices.Speech.1.14.0-alpha.0.15922490.nupkg -source c:\local-nuget-feed```
1. You can now delete the .nupkg file you downloaded (```c:\temp\Microsoft.CognitiveServices.Speech.1.14.0-alpha.0.15922490.nupkg``` in this example)
1. Open SPX.sln, and "Manage NuGet Packages". Uninstall the public Speech SDK Nuget (Microsoft.CognitiveServices.Speech)
1. Open the gears icon in the NuGet Package Manager, and click on the plus icon to add a new feed. Name it ```Local``` (or anything you want), and point to the new local feed folder ```c:\local-nuget-feed```. 
1. Make sure this feed is listed before nuget.org. Use the up and down arrow to move the new local feed above nuget.org. Press OK. **Note:** I (Darren) could not get the local feed to stay above nuget.org feed. It would not stick. I had to edit the file NuGet.Config manually to put my local feed first in the XML file.
1. Now back in the NuGet Package Manager, switch to ```Local``` feed, and in the ```Browse``` tab, check the ```show prerelease``` box. It should now show your the local ```Microsoft.CognitiveServices.Speech``` package. Install it.
1. Compile the Visual Studio project, debug and run as you would normally do.
1. When you are done working and want to remove this local feed, run: ```nuget.exe delete Microsoft.CognitiveServices.Speech 1.14.0-alpha.0.15922490  -source c:\local-nuget-feed```
1. Note that in case of embedded (SR/TTS) binaries, this will still require copying files from Carbon and embedded engine build artifacts if there are no .nupkg packages for them yet.
