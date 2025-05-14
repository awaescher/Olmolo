#addin "Cake.Git&version=1.0.0"
#addin nuget:?package=Cake.Docker&version=1.3.0
#tool "nuget:?package=GitVersion.CommandLine&version=4.0.0"

var target = Argument("target", "Test");
var configuration = Argument("configuration", "Release");

var containerRegistry = "";
var rootNamespace = "awaescher";
var projectTag = "olmolo";
var publishRuntime = "linux-x64"; // win-x64, linux-x64, linux-arm64, etc., see https://learn.microsoft.com/de-de/dotnet/core/rid-catalog
var containerPlatform = "linux/amd64"; // linux/amd64, linux/arm64, etc., see https://docs.docker.com/build/building/multi-platform/

var fullVersion = "";
var semVersion = "";


//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("SetVersion")
       .Does(() =>
    {
        var gitVersion = GitVersion(new GitVersionSettings());

        var commits = GitLog(".", int.MaxValue);

        var buildNumber = commits.Count.ToString();
        fullVersion = gitVersion.AssemblySemVer;
        semVersion = gitVersion.SemVer;

        Information($"FullVersion:\t{fullVersion}");
        Information($"AssemblySemFileVer:\t{gitVersion.AssemblySemFileVer}");
        Information($"AssemblySemVer:\t{gitVersion.AssemblySemVer}");
        Information($"BranchName:\t{gitVersion.BranchName}");
        Information($"BuildMetaData:\t{gitVersion.BuildMetaData}");
        Information($"BuildMetaDataPadded:\t{gitVersion.BuildMetaDataPadded}");
        Information($"FullBuildMetaData:\t{gitVersion.FullBuildMetaData}");
        Information($"FullSemVer:\t{gitVersion.FullSemVer}");
        Information($"InformationalVersion:\t{gitVersion.InformationalVersion}");
        Information($"PreReleaseLabel:\t{gitVersion.PreReleaseLabel}");
        Information($"SemVer:\t{gitVersion.SemVer}");
        Information($"Build {buildNumber} (= number of commits in git history)");
    });

Task("Build")
    .IsDependentOn("SetVersion")
    .Does(() =>
{
    DotNetBuild("./Olmolo.sln", new DotNetBuildSettings
    {
        Configuration = configuration,
        MSBuildSettings = new DotNetMSBuildSettings { Version = fullVersion }
    });
});

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
{
    DotNetTest("./Olmolo.sln", new DotNetTestSettings
    {
        Configuration = configuration,
        NoBuild = true,
    });
});

Task("BuildContainer")
    .IsDependentOn("Test")
    .Does(() =>
{
    var packageName = $"{rootNamespace.ToLower()}/{projectTag.ToLower()}";
    string[] tags = new string[] { $"{packageName}:{fullVersion}", $"{packageName}:latest" };
    string[] args = new string[]
    {
         $"PUBLISH_RUNTIME={publishRuntime}",
         $"VERSION={fullVersion}",
         $"BUILD_CONFIGURATION={configuration}",
    };

    Information($"Building container {string.Join(", ", tags)} ...");

    var settings = new DockerImageBuildSettings { Platform = containerPlatform, Tag = tags, BuildArg = args };
    DockerBuild(settings, "./");
});

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);