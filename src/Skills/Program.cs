using Skills.Commands;
using Skills.Extensions;

namespace Skills;

internal static class Program
{
    public static Task<int> Main(string[] args) => RunAsync(args, toolCommandName: null);

    internal static async Task<int> RunAsync(string[] args, string? toolCommandName)
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

    // Strips bare `--` tokens. The CLI has no pass-through commands, so the argument terminator
    // is meaningless here and would only confuse System.CommandLine's parsing.
    internal static string[] StripBareTerminators(IReadOnlyList<string> args) => args.Where(a => a != "--").ToArray();
}
