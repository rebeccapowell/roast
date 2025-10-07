using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;

namespace CoffeeTalk.TestUtilities;

internal static class DockerRequirement
{
    private static readonly Lazy<DockerRequirementResult> Detection = new(DetectDockerAvailability, LazyThreadSafetyMode.ExecutionAndPublication);

    public static DockerRequirementResult Current => Detection.Value;

    private static DockerRequirementResult DetectDockerAvailability()
    {
        if (!IsEnvironmentSupported())
        {
            return new DockerRequirementResult(false, false, "Docker is not supported in this environment.");
        }

        var (isAvailable, reason) = TryProbeDocker();
        return new DockerRequirementResult(true, isAvailable, reason);
    }

    private static bool IsEnvironmentSupported()
    {
        if (OperatingSystem.IsLinux())
        {
            return true;
        }

        return !PlatformDetection.IsRunningOnCi;
    }

    private static (bool IsAvailable, string? Reason) TryProbeDocker()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            });

            if (process is null)
            {
                return (false, "Unable to start docker process.");
            }

            if (!process.WaitForExit(2000))
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                }

                return (false, "Timed out while checking docker availability.");
            }

            if (process.ExitCode != 0)
            {
                return (false, "Docker CLI returned a non-zero exit code.");
            }

            return (true, null);
        }
        catch (Win32Exception)
        {
            return (false, "Docker CLI is not installed or not available on PATH.");
        }
        catch (Exception ex)
        {
            return (false, $"Docker CLI check failed: {ex.Message}");
        }
    }
}

internal readonly record struct DockerRequirementResult(bool IsEnvironmentSupported, bool IsDockerAvailable, string? Reason);
