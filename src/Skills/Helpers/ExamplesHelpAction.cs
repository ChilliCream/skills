using System.CommandLine.Help;
using System.CommandLine.Invocation;
using System.Runtime.CompilerServices;

namespace Skills;

/// <summary>
/// Attaches example command lines to a <see cref="Command"/> so they render under the default
/// help output. Commands opt in with <c>this.AddExamples("add owner/repo")</c>; the examples are
/// looked up by <see cref="ExamplesHelpAction"/> once help actually renders.
/// </summary>
internal static class CommandExamples
{
    private static readonly ConditionalWeakTable<Command, string[]> s_examples = [];

    public static void AddExamples(Command command, string[] examples) => s_examples.AddOrUpdate(command, examples);

    public static bool TryGetExamples(Command command, out string[]? examples) => s_examples.TryGetValue(command, out examples);

    /// <summary>
    /// Wraps the root command's help option so its default <see cref="HelpAction"/> is followed by
    /// the invoked command's examples, if any were registered via <see cref="AddExamples"/>.
    /// </summary>
    public static void Install(RootCommand rootCommand)
    {
        for (var i = 0; i < rootCommand.Options.Count; i++)
        {
            if (rootCommand.Options[i] is HelpOption helpOption && helpOption.Action is HelpAction helpAction)
            {
                helpOption.Action = new ExamplesHelpAction(helpAction);
                return;
            }
        }
    }
}

internal sealed class ExamplesHelpAction(HelpAction defaultHelp) : SynchronousCommandLineAction
{
    public override int Invoke(ParseResult parseResult)
    {
        var result = defaultHelp.Invoke(parseResult);
        var command = parseResult.CommandResult.Command;

        if (CommandExamples.TryGetExamples(command, out var examples) && examples is not null)
        {
            // Help can render before a command action ever runs (it is its own HelpOption
            // action), so this uses the null-tolerant TryResolve and falls back to the default
            // command name rather than throwing.
            var services = CommandServicesResolver.TryResolve(command);
            var commandName = services?.GetRequiredService<CliExecutionContext>().CommandName ?? "skills";

            var output = parseResult.InvocationConfiguration.Output;
            output.WriteLine("Example:");
            foreach (var example in examples)
            {
                output.WriteLine($"  {commandName} {example}");
            }
            output.WriteLine();
        }

        return result;
    }
}
