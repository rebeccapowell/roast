using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace CoffeeTalk.AppHost.Extensions;

internal static class ExecutableResourceBuilderExtensions
{
    public static IResourceBuilder<ExecutableResource> WithExplicitStart(this IResourceBuilder<ExecutableResource> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithAnnotation(new ExplicitStartupAnnotation());
    }

    public static IResourceBuilder<ExecutableResource> WithPlaywrightRepeatCommand(
        this IResourceBuilder<ExecutableResource> builder,
        IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(args);

        var commandOptions = new CommandOptions
        {
            IconName = "ArrowRepeatAll",
            IsHighlighted = true,
        };

        return builder.WithCommand(
            name: "repeat-playwright-tests",
            displayName: "Repeat Playwright Tests",
            executeCommand: async context =>
            {
                var interactionService = context.ServiceProvider.GetRequiredService<IInteractionService>();
                var prompt = await interactionService.PromptInputAsync(
                    title: "Repetition",
                    message: "How many times do you want to repeat the Playwright tests?",
                    input: new InteractionInput
                    {
                        Name = "RepetitionCount",
                        Label = "Repetition Count",
                        Description = "Enter the number of times to repeat the Playwright tests.",
                        InputType = InputType.Number,
                        Required = true,
                        Placeholder = "1",
                    });

                if (prompt.Canceled)
                {
                    return CommandResults.Success();
                }

                if (!int.TryParse(prompt.Data?.Value, out var repetitions) || repetitions <= 0)
                {
                    return new ExecuteCommandResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid repetition count.",
                    };
                }

                var loggerService = context.ServiceProvider.GetRequiredService<ResourceLoggerService>();
                var logger = loggerService.GetLogger(context.ResourceName);

                for (var iteration = 1; iteration <= repetitions; iteration++)
                {
                    logger.LogInformation("Starting Playwright run {Iteration} of {Total}.", iteration, repetitions);

                    using var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = builder.Resource.Command,
                            WorkingDirectory = builder.Resource.WorkingDirectory,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                        },
                    };

                    foreach (var arg in args)
                    {
                        process.StartInfo.ArgumentList.Add(arg);
                    }

                    try
                    {
                        process.Start();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to start Playwright process.");
                        return new ExecuteCommandResult
                        {
                            Success = false,
                            ErrorMessage = ex.Message,
                        };
                    }

                    var outputTask = ReadLinesAsync(process.StandardOutput, line =>
                        logger.LogInformation("[Run {Iteration}] {Line}", iteration, line));
                    var errorTask = ReadLinesAsync(process.StandardError, line =>
                        logger.LogError("[Run {Iteration}] {Line}", iteration, line));

                    await process.WaitForExitAsync(context.CancellationToken);
                    await Task.WhenAll(outputTask, errorTask);

                    if (process.ExitCode != 0)
                    {
                        logger.LogError("Playwright run {Iteration} failed with exit code {ExitCode}.", iteration, process.ExitCode);
                        return new ExecuteCommandResult
                        {
                            Success = false,
                            ErrorMessage = $"Run failed with exit code {process.ExitCode}.",
                        };
                    }
                }

                return CommandResults.Success();
            },
            commandOptions);
    }

    private static Task ReadLinesAsync(TextReader reader, Action<string> log)
    {
        return Task.Run(async () =>
        {
            string? line;
            while ((line = await reader.ReadLineAsync()) is not null)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    log(line);
                }
            }
        });
    }
}

#pragma warning restore ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
