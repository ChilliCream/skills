using Skills.Interaction;

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
            var services = CommandExecutionContext.s_services.Value!;
            var interaction = services.GetRequiredService<IInteractionService>();

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
                    interaction.WriteError(exception.Message);
                }

                return exception.ExitCode;
            }
            catch (Exception ex) when (ex is OperationCanceledException or TaskCanceledException)
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
