namespace Skills.Commands;

/// <summary>
/// Composes the five skills verbs onto a command. The sole reason this exists: both
/// <see cref="SkillsCommand"/> (the embeddable group) and the internal <c>SkillsRootCommand</c>
/// (the standalone entry point) must carry the identical set of subcommands, and a hand-kept
/// second list is exactly the drift this type prevents.
/// </summary>
internal static class SkillsSubcommands
{
    public static void AddTo(Command command)
    {
        command.Subcommands.Add(new AddCommand());
        command.Subcommands.Add(new RemoveCommand());
        command.Subcommands.Add(new ListCommand());
        command.Subcommands.Add(new InitCommand());
        command.Subcommands.Add(new UpdateCommand());
    }
}
