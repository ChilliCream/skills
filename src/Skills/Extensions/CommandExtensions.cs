using Skills.Interaction;
using Skills.Options;

namespace Skills.Extensions;

/// <summary>
/// Provides shared command configuration helpers.
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

            // Reset the format on every invocation because hosts may reuse the interaction service.
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
    /// Adds example command lines to the command's help output.
    /// </summary>
    public static Command AddExamples(this Command command, params string[] examples)
    {
        CommandExamples.AddExamples(command, examples);
        return command;
    }
}
