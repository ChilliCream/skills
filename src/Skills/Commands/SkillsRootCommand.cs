namespace Skills.Commands;

/// <summary>
/// The standalone <c>skills</c> executable's root command. Composes the same five subcommands as
/// <see cref="SkillsCommand"/>, via the shared <see cref="SkillsSubcommands"/> helper, so the two
/// entry points never drift apart.
/// </summary>
internal sealed class SkillsRootCommand : RootCommand
{
    public SkillsRootCommand() : base("Skills - AI agent skill manager")
    {
        SkillsSubcommands.AddTo(this);

        CommandExamples.Install(this);
    }
}
