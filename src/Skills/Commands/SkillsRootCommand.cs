namespace Skills.Commands;

internal sealed class SkillsRootCommand : RootCommand
{
    public SkillsRootCommand() : base("Skills - AI agent skill manager")
    {
        Subcommands.Add(new AddCommand());
        Subcommands.Add(new RemoveCommand());
        Subcommands.Add(new ListCommand());
        Subcommands.Add(new InitCommand());
        Subcommands.Add(new UpdateCommand());
    }
}
