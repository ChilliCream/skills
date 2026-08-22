namespace Skills;

/// <summary>
/// Static factories for the common <see cref="ExitException"/> shapes a command throws when it
/// cannot continue. Centralizing the message wording here keeps call sites short and consistent.
/// </summary>
internal static class ThrowHelper
{
    public static ExitException Exit(string message) => new(message);

    public static ExitException MissingRequiredOption(string optionName)
        => Exit($"Missing required option '{optionName}'.");

    public static ExitException MissingRequiredArgument(string argumentName)
        => Exit($"Missing required argument '{argumentName}'.");
}
