using Skills.Interaction;
using Skills.Options;

namespace Skills.Extensions;

/// <summary>
/// Wires a command's action so any exception it throws is caught here, reported through
/// <see cref="IInteractionService"/>, and turned into an exit code instead of crashing the
/// process. Commands call <c>this.SetActionWithExceptionHandling(ExecuteAsync)</c> instead of
/// <c>SetAction</c> directly, so the catch ladder lives in one place rather than being repeated
/// per command.
/// </summary>
internal static class CommandExtensions
{
    public static Command SetActionWithExceptionHandling(
        this Command command,
        Func<ICommandServices, ParseResult, CancellationToken, Task<int>> action)
    {
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            // Resolved from the command tree the caller built rather than ambient state, so this
            // runs identically whether the command was parsed under the standalone
            // SkillsRootCommand or a host's own root that embedded SkillsCommand. A host that
            // forgot to compose under either gets a named diagnostic here, not a null dereference.
            var services = CommandServicesResolver.Resolve(parseResult.CommandResult.Command);
            var interaction = services.GetRequiredService<IInteractionService>();

            // Resolved here rather than in RootCommandExtensions.ExecuteAsync (which a host
            // pipeline never runs) so the --json/--format json switch behaves identically whether
            // the command runs standalone or embedded. Set unconditionally on every invocation
            // (Json when flagged, null otherwise) rather than only when flagged: interaction is a
            // singleton in an embedding host, so a prior invocation's Json mode must not leak into
            // one that did not ask for it.
            var format = parseResult.GetValue(Opt<OptionalOutputFormatOption>.Instance);
            var jsonFlag = parseResult.GetValue(Opt<JsonOption>.Instance);
            interaction.SetOutputFormat(jsonFlag || format == "json" ? OutputFormat.Json : null);

            try
            {
                return await action.Invoke(services, parseResult, cancellationToken);
            }
            catch (ExitException exception)
            {
                if (!string.IsNullOrEmpty(exception.Message))
                {
                    interaction.WriteError(exception.Message);
                }
            }
            catch (CliException exception)
            {
                if (exception.Title is { } title)
                {
                    interaction.WriteErrorPanel(title, exception.Message, exception.Hint);
                }
                else if (!string.IsNullOrEmpty(exception.Message))
                {
                    // WriteError has no hint parameter, so a hint on a title-less exception is
                    // dropped here rather than rendered. Every current call site that sets a hint
                    // also sets a title, so this arm never sees one in practice.
                    interaction.WriteError(exception.Message);
                }

                return exception.ExitCode;
            }
            catch (OperationCanceledException)
            {
                return ExitCodeConstants.Cancelled;
            }
            catch (Exception ex)
            {
                interaction.WriteError($"There was an unexpected error: {ex.Message}");
            }

            return ExitCodeConstants.Failure;
        });

        return command;
    }

    /// <summary>
    /// Registers example command lines to render under this command's <c>--help</c> output.
    /// Each example is the command's arguments/options as typed, without the command name itself
    /// (for example <c>"owner/repo --skill foo -a claude-code"</c> for <c>add</c>).
    /// </summary>
    public static Command AddExamples(this Command command, params string[] examples)
    {
        CommandExamples.AddExamples(command, examples);
        return command;
    }
}
