using System.Collections.Immutable;

namespace Skills.Commands;

/// <summary>
/// Shared agent-name validation used by every command that accepts an <c>--agent</c> filter, so
/// an unrecognized agent name is reported the same way everywhere: a titled error with a sorted
/// list of valid agents as the hint.
/// </summary>
internal static class AgentValidation
{
    /// <summary>
    /// Throws a titled <see cref="CliException"/> if any entry in <paramref name="agents"/> is not
    /// present in <paramref name="validAgents"/>.
    /// </summary>
    public static void EnsureValidAgents(IReadOnlyCollection<string> agents, ImmutableArray<string> validAgents)
    {
        var invalid = agents.Where(a => !validAgents.Contains(a)).ToList();
        if (invalid.Count == 0)
        {
            return;
        }

        // Sort the advisory list so the hint is deterministic and easy to scan; the registry's
        // own order is hash-bucket order and not meaningful to the user.
        var sortedValid = validAgents.OrderBy(a => a, StringComparer.Ordinal);
        throw new CliException(
            ExitCodeConstants.Failure,
            $"Invalid agents: {invalid.Join(", ")}",
            title: "Invalid agents",
            hint: $"Valid agents: {sortedValid.Join(", ")}");
    }
}
