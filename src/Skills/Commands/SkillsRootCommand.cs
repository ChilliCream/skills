namespace Skills.Commands;

/// <summary>
/// The standalone <c>skills</c> executable's root command. Composes the same five subcommands as
/// <see cref="SkillsCommand"/>, via the shared <see cref="SkillsSubcommands"/> helper, so the two
/// entry points never drift apart.
/// </summary>
internal sealed class SkillsRootCommand : RootCommand, ICommandServicesSource
{
    private readonly ICommandServices _commandServices;

    public SkillsRootCommand(IServiceProvider serviceProvider) : base("Skills - AI agent skill manager")
    {
        _commandServices = new CommandServices(serviceProvider);
        SkillsSubcommands.AddTo(this);

        CommandExamples.Install(this);
    }

    ICommandServices ICommandServicesSource.CommandServices => _commandServices;
}
