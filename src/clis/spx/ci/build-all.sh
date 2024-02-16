SpeechSDKVersion=1.21.1
spx_version=1.21.1
spx_version_suffix=-dev
BuildConfiguration=Release
Build_ArtifactStagingDirectory=bin\\artifacts

dotnet restore "spx-cli.csproj" /p:VersionAssembly=$spx_version /p:Version=$spx_version /p:AssemblyVersion=$spx_version /p:VersionSuffix=$spx_version_suffix /p:SpeechSDKVersion=$SpeechSDKVersion -r linux-x64
dotnet build   "spx-cli.csproj" /p:VersionAssembly=$spx_version /p:Version=$spx_version /p:AssemblyVersion=$spx_version /p:VersionSuffix=$spx_version_suffix --version-suffix $spx_version_suffix /p:SpeechSDKVersion=$SpeechSDKVersion -r linux-x64 -c $BuildConfiguration --no-restore            /p:IncludeSymbols=false
dotnet publish "spx-cli.csproj" /p:VersionAssembly=$spx_version /p:Version=$spx_version /p:AssemblyVersion=$spx_version /p:VersionSuffix=$spx_version_suffix --version-suffix $spx_version_suffix /p:SpeechSDKVersion=$SpeechSDKVersion -r linux-x64 -c $BuildConfiguration --no-restore --no-build /p:PublishProfile="Properties\PublishProfiles\folder publish spx-cli release (linux-x64).pubxml"

dotnet restore "spx-cli.csproj" /p:VersionAssembly=$spx_version /p:Version=$spx_version /p:AssemblyVersion=$spx_version /p:VersionSuffix=$spx_version_suffix /p:SpeechSDKVersion=$SpeechSDKVersion -r win-x64
dotnet build   "spx-cli.csproj" /p:VersionAssembly=$spx_version /p:Version=$spx_version /p:AssemblyVersion=$spx_version /p:VersionSuffix=$spx_version_suffix --version-suffix $spx_version_suffix /p:SpeechSDKVersion=$SpeechSDKVersion -r win-x64   -c $BuildConfiguration --no-restore
dotnet publish "spx-cli.csproj" /p:VersionAssembly=$spx_version /p:Version=$spx_version /p:AssemblyVersion=$spx_version /p:VersionSuffix=$spx_version_suffix --version-suffix $spx_version_suffix /p:SpeechSDKVersion=$SpeechSDKVersion -r win-x64   -c $BuildConfiguration --no-restore --no-build /p:IncludeSymbols=false /p:PublishProfile="Properties\PublishProfiles\folder publish spx-cli release (win-x64).pubxml"

dotnet pack    "spx-cli.csproj" /p:Version=$spx_version /p:PackageVersion=$spx_version --version-suffix $spx_version_suffix -c $BuildConfiguration -o $Build_ArtifactStagingDirectory/nupkg -p:IncludeSymbols=false --no-restore

ll $Build_ArtifactStagingDirectory

