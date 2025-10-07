using System;
using Xunit;

namespace CoffeeTalk.TestUtilities;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequiresDockerFactAttribute : FactAttribute
{
    public RequiresDockerFactAttribute(string? skipReason = null)
    {
        var result = DockerRequirement.Current;

        if (!result.IsEnvironmentSupported)
        {
            Skip = skipReason ?? result.Reason ?? "Docker is not supported in this environment.";
            return;
        }

        if (!result.IsDockerAvailable)
        {
            Skip = skipReason ?? result.Reason ?? "Docker runtime is not available.";
        }
    }
}
