namespace Skills;

internal static class CommandExecutionContext
{
    internal static readonly AsyncLocal<ICommandServices> s_services = new();
}
