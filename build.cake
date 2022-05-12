public record BuildData(
    DirectoryPath SourcePath,
    string Configuration,
    DirectoryPath ArtifactsPath,
    string Version
)
{
    public DotNetMSBuildSettings MSBuildSettings { get; } = new DotNetMSBuildSettings{
        Version = Version
    }
                                        .SetConfiguration(Configuration);
}


Setup(
    context => new BuildData(
        "./src",
        "Release",
        "./artifacts",
        GitHubActions.IsRunningOnGitHubActions
            ? FormattableString.Invariant($"{DateTime.Now:yyyy.MM.dd}.{GitHubActions.Environment.Workflow.RunNumber}")
            : "1.0.0.0"
        )
);

Task("Clean")
    .Does<BuildData>(
        (context, data)=> CleanDirectory(data.ArtifactsPath)
    );
Task("Restore")
    .IsDependentOn("Clean")
    .Does<BuildData>(
        (context, data)=>DotNetRestore(
            data.SourcePath.FullPath,
            new DotNetRestoreSettings {
                MSBuildSettings = data.MSBuildSettings
            }
            ));

Task("Build")
    .IsDependentOn("Restore")
        .Does<BuildData>(
        (context, data)=>DotNetBuild(data.SourcePath.FullPath,
        new DotNetBuildSettings {
            NoRestore = true,
            MSBuildSettings = data.MSBuildSettings
        }));

Task("Test")
    .IsDependentOn("Build")
    .Does<BuildData>(
        (context, data)=>DotNetTest(
            data.SourcePath.FullPath,
            new DotNetTestSettings
            {
                NoRestore= true,
                NoBuild = true,
                Configuration= data.Configuration
            }
        ));

Task("Package")
    .IsDependentOn("Test")
     .Does<BuildData>(
        (context, data)=>DotNetPack(
            data.SourcePath.FullPath,
            new DotNetPackSettings {
                NoRestore = false,
                NoBuild = false,
                MSBuildSettings = data.MSBuildSettings,
                OutputDirectory = data.ArtifactsPath
            }
        ));

Task("Publish")
    .IsDependentOn("Package")
    .Does<BuildData>(
        (context, data)=> GitHubActions.Commands.UploadArtifact(
            data.ArtifactsPath,
            $"HelloWorld{Context.Environment.Platform.Family}"
            ));

Task("Default")
    .IsDependentOn("Package");

Task("GitHubActions")
    .IsDependentOn("Publish");


RunTarget(Argument("target", "Default"));