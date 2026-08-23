namespace Skills;

/// <summary>
/// Resolves the <see cref="ICommandServices"/> for an invoked command by walking up
/// <see cref="Symbol.Parents"/> from the command to the nearest ancestor implementing
/// <see cref="ICommandServicesSource"/>. Replaces the old ambient <c>AsyncLocal</c>: services are
/// found from the actual command tree a host built, so a host that never composed under
/// <c>SkillsCommand</c> or the internal <c>SkillsRootCommand</c> gets a named diagnostic instead
/// of a null dereference deep inside a handler.
/// </summary>
internal static class CommandServicesResolver
{
    /// <summary>
    /// Resolves the services for <paramref name="command"/>, throwing when no ancestor supplies
    /// them.
    /// </summary>
    public static ICommandServices Resolve(Command command)
        => TryResolve(command) ?? throw new InvalidOperationException(
            $"{command.Name} must be composed under SkillsCommand or SkillsRootCommand "
            + "constructed with the IServiceProvider built from AddSkillsServices.");

    /// <summary>
    /// Resolves the services for <paramref name="symbol"/>, returning <see langword="null"/>
    /// instead of throwing when no ancestor supplies them.
    /// </summary>
    public static ICommandServices? TryResolve(Symbol symbol)
    {
        if (symbol is ICommandServicesSource source)
        {
            return source.CommandServices;
        }

        foreach (var parent in symbol.Parents)
        {
            if (TryResolve(parent) is { } services)
            {
                return services;
            }
        }

        return null;
    }
}
