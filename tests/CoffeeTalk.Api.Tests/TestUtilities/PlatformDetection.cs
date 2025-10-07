using System;

namespace CoffeeTalk.TestUtilities;

internal static class PlatformDetection
{
    public static bool IsRunningOnAzdoBuildMachine => Environment.GetEnvironmentVariable("BUILD_BUILDID") is not null;
    public static bool IsRunningOnHelix => Environment.GetEnvironmentVariable("HELIX_WORKITEM_ROOT") is not null;
    public static bool IsRunningOnGithubActions => Environment.GetEnvironmentVariable("GITHUB_JOB") is not null;
    public static bool IsRunningOnCi => IsRunningOnAzdoBuildMachine || IsRunningOnHelix || IsRunningOnGithubActions;
}
