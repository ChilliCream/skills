namespace Skills;

/// <summary>
/// Signals that a command should stop and exit, without representing an unexpected failure. When
/// the message is empty, the command already reported the necessary detail through
/// <c>IInteractionService</c> before throwing, so the catch ladder in
/// <c>CommandExtensions.SetActionWithExceptionHandling</c> writes nothing further.
/// </summary>
internal sealed class ExitException : Exception
{
    public ExitException() : base("")
    {
    }

    public ExitException(string message) : base(message)
    {
    }
}
