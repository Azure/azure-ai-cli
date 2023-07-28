set VisionSDKVersion=1.21.1
set vz_version=1.21.1
set vz_version_suffix=-dev
set BuildConfiguration=Release
set Build_ArtifactStagingDirectory=bin\artifacts

dotnet restore "vz-cli.csproj" /p:VersionAssembly=%vz_version% /p:Version=%vz_version% /p:AssemblyVersion=%vz_version% /p:VersionSuffix=%vz_version_suffix% /p:VisionSDKVersion=%VisionSDKVersion% -r linux-x64
dotnet build   "vz-cli.csproj" /p:VersionAssembly=%vz_version% /p:Version=%vz_version% /p:AssemblyVersion=%vz_version% /p:VersionSuffix=%vz_version_suffix% --version-suffix %vz_version_suffix% /p:VisionSDKVersion=%VisionSDKVersion% -r linux-x64 -c %BuildConfiguration% --no-restore            /p:IncludeSymbols=false
dotnet publish "vz-cli.csproj" /p:VersionAssembly=%vz_version% /p:Version=%vz_version% /p:AssemblyVersion=%vz_version% /p:VersionSuffix=%vz_version_suffix% --version-suffix %vz_version_suffix% /p:VisionSDKVersion=%VisionSDKVersion% -r linux-x64 -c %BuildConfiguration% --no-restore --no-build /p:PublishProfile="Properties\PublishProfiles\folder publish vz-cli release (linux-x64).pubxml" 

dotnet restore "vz-cli.csproj" /p:VersionAssembly=%vz_version% /p:Version=%vz_version% /p:AssemblyVersion=%vz_version% /p:VersionSuffix=%vz_version_suffix% /p:VisionSDKVersion=%VisionSDKVersion% -r win-x64
dotnet build   "vz-cli.csproj" /p:VersionAssembly=%vz_version% /p:Version=%vz_version% /p:AssemblyVersion=%vz_version% /p:VersionSuffix=%vz_version_suffix% --version-suffix %vz_version_suffix% /p:VisionSDKVersion=%VisionSDKVersion% -r win-x64   -c %BuildConfiguration% --no-restore 
dotnet publish "vz-cli.csproj" /p:VersionAssembly=%vz_version% /p:Version=%vz_version% /p:AssemblyVersion=%vz_version% /p:VersionSuffix=%vz_version_suffix% --version-suffix %vz_version_suffix% /p:VisionSDKVersion=%VisionSDKVersion% -r win-x64   -c %BuildConfiguration% --no-restore --no-build /p:IncludeSymbols=false /p:PublishProfile="Properties\PublishProfiles\folder publish vz-cli release (win-x64).pubxml" 

dotnet pack    "vz-cli.csproj" /p:Version=%vz_version% /p:PackageVersion=%vz_version% --version-suffix %vz_version_suffix% -c %BuildConfiguration% -o %Build_ArtifactStagingDirectory%/nupkg -p:IncludeSymbols=false --no-restore

dir %Build_ArtifactStagingDirectory%

