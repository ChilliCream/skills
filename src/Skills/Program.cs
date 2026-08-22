using Skills.Commands;
using Skills.Extensions;

namespace Skills;

/// <summary>
/// Runs the skills CLI end to end. <see cref="RunAsync"/> is the entry point the standalone
/// <c>skills</c> executable (src/Skills.Cli) and the <c>dotnet tool</c> wrapper (src/Skills.Tool)
/// both call; it builds the service container, parses the arguments against
/// <see cref="SkillsRootCommand"/>, and maps the outcome to a process exit code.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs the skills CLI.
    /// </summary>
    /// <param name="args">The raw command-line arguments.</param>
    /// <param name="toolCommandName">
    /// The prefix under which the CLI is invoked (for example <c>skills</c> or <c>skillz</c>);
    /// <see langword="null"/> or blank falls back to <c>"skills"</c>.
    /// </param>
    public static async Task<int> RunAsync(string[] args, string? toolCommandName)
    {
        toolCommandName = string.IsNullOrWhiteSpace(toolCommandName) ? "skills" : toolCommandName.Trim();

        using var cts = new CancellationTokenSource();

        ConsoleCancelEventHandler cancelKeyHandler = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cts.Cancel();
        };
        Console.CancelKeyPress += cancelKeyHandler;

        EventHandler unloadHandler = (_, _) => { if (!cts.IsCancellationRequested)
        {
            cts.Cancel();
        } };
        AppDomain.CurrentDomain.ProcessExit += unloadHandler;

        var services = new ServiceCollection();
        services.AddSkillsServices(toolCommandName);

        try
        {
            await using var provider = services.BuildServiceProvider();

            var rootCommand = new SkillsRootCommand();

            return await rootCommand.ExecuteAsync(args, provider, invocationConfiguration: null, cts.Token);
        }
        catch (OperationCanceledException)
        {
            return ExitCodeConstants.Cancelled;
        }
        catch (CliException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return ex.ExitCode;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return ExitCodeConstants.Failure;
        }
        finally
        {
            Console.CancelKeyPress -= cancelKeyHandler;
            AppDomain.CurrentDomain.ProcessExit -= unloadHandler;
        }
    }
}
