using System.Collections;
using System.Diagnostics;
using System.IO;

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, args) =>
{
  args.Cancel = true;
  cts.Cancel();
};

var workingDirectory = ResolveProjectDirectory();
await EnsureDependenciesAsync(workingDirectory, cts.Token);

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
    ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
    ?? "Development";
var script = string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase) ? "dev" : "start";

if (!string.Equals(script, "dev", StringComparison.OrdinalIgnoreCase))
{
  await RunProcessAsync("npm", new[] { "run", "build" }, workingDirectory, cts.Token);
}

await RunProcessAsync("npm", new[] { "run", script }, workingDirectory, cts.Token);

static async Task EnsureDependenciesAsync(string workingDirectory, CancellationToken cancellationToken)
{
  if (Directory.Exists(Path.Combine(workingDirectory, "node_modules")))
  {
    return;
  }

  await RunProcessAsync("npm", new[] { "install" }, workingDirectory, cancellationToken);
}

static async Task RunProcessAsync(string command, IEnumerable<string> args, string workingDirectory, CancellationToken cancellationToken)
{
  using var process = CreateProcess(command, args, workingDirectory);
  process.Start();
  process.BeginOutputReadLine();
  process.BeginErrorReadLine();

  using var registration = cancellationToken.Register(() =>
  {
    if (!process.HasExited)
    {
      try
      {
        process.Kill(entireProcessTree: true);
      }
      catch
      {
        // ignored
      }
    }
  });

  await process.WaitForExitAsync();

  if (process.ExitCode != 0 && !cancellationToken.IsCancellationRequested)
  {
    throw new InvalidOperationException($"Command '{command} {string.Join(' ', args)}' failed with exit code {process.ExitCode}.");
  }
}

static Process CreateProcess(string command, IEnumerable<string> args, string workingDirectory)
{
  var process = new Process
  {
    StartInfo = new ProcessStartInfo
    {
      FileName = ResolveCommand(command),
      WorkingDirectory = workingDirectory,
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false,
    },
    EnableRaisingEvents = true,
  };

  ApplyEnvironmentVariables(process.StartInfo);

  foreach (var arg in args)
  {
    process.StartInfo.ArgumentList.Add(arg);
  }

  process.OutputDataReceived += (_, e) =>
  {
    if (e.Data is not null)
    {
      Console.Out.WriteLine(e.Data);
    }
  };

  process.ErrorDataReceived += (_, e) =>
  {
    if (e.Data is not null)
    {
      Console.Error.WriteLine(e.Data);
    }
  };

  return process;
}

static string ResolveCommand(string command)
{
  return OperatingSystem.IsWindows() ? $"{command}.cmd" : command;
}

static void ApplyEnvironmentVariables(ProcessStartInfo startInfo)
{
  foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
  {
    if (entry.Key is string key && entry.Value is string value)
    {
      startInfo.Environment[key] = value;
    }
  }
}

static string ResolveProjectDirectory()
{
  var current = AppContext.BaseDirectory;

  while (!string.IsNullOrEmpty(current))
  {
    if (File.Exists(Path.Combine(current, "package.json")))
    {
      return current;
    }

    var parent = Directory.GetParent(current);
    current = parent?.FullName ?? string.Empty;
  }

  throw new InvalidOperationException("Failed to locate the CoffeeTalk.Web project directory.");
}
