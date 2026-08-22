using Skills.Arguments;
using Skills.Commands.Add.Options;
using Skills.Extensions;
using Skills.Interaction;
using Skills.Options;

namespace Skills.Commands;

internal sealed class AddCommand : Command
{
    public AddCommand() : base("add", "Add a skill from a source")
    {
        Arguments.Add(Opt<OptionalSourceArgument>.Instance);
        Options.Add(Opt<GlobalOption>.Instance);
        Options.Add(Opt<AgentOption>.Instance);
        Options.Add(Opt<SkillOption>.Instance);
        Options.Add(Opt<YesOption>.Instance);
        Options.Add(Opt<AllOption>.Instance);
        Options.Add(Opt<CopyOption>.Instance);
        Options.Add(Opt<FullDepthOption>.Instance);
        Options.Add(Opt<ListOption>.Instance);

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var interaction = services.GetRequiredService<IInteractionService>();
        var executor = services.GetRequiredService<AddCommandExecutor>();
        var executionContext = services.GetRequiredService<CliExecutionContext>();

        var options = ParseOptions(parseResult);

        if (string.IsNullOrWhiteSpace(options.Source))
        {
            interaction.WriteError("Missing required argument: source");
            interaction.WriteLine($"Usage: {executionContext.CommandName} add <source> [options]");
            return ExitCodeConstants.Failure;
        }

        return await executor.RunAsync(options, cancellationToken);
    }

    private static AddCommandOptions ParseOptions(ParseResult parseResult)
    {
        var source = parseResult.GetValue(Opt<OptionalSourceArgument>.Instance);
        var global = parseResult.GetValue(Opt<GlobalOption>.Instance);
        var agents = parseResult.GetValue(Opt<AgentOption>.Instance) ?? [];
        var skills = parseResult.GetValue(Opt<SkillOption>.Instance) ?? [];
        var yes = parseResult.GetValue(Opt<YesOption>.Instance);
        var all = parseResult.GetValue(Opt<AllOption>.Instance);
        var copy = parseResult.GetValue(Opt<CopyOption>.Instance);
        var fullDepth = parseResult.GetValue(Opt<FullDepthOption>.Instance);
        var list = parseResult.GetValue(Opt<ListOption>.Instance);

        if (all)
        {
            skills = ["*"];
            agents = ["*"];
            yes = true;
        }

        return new AddCommandOptions(source, global, [.. agents], [.. skills], yes, all, copy, fullDepth, list);
    }
}
