namespace Skills.Commands;

/// <summary>
/// The embeddable skills command group. A host adds this as a subcommand of its own root command
/// and gets <c>&lt;host&gt; skills add|remove|list|init|update</c>.
/// </summary>
public sealed class SkillsCommand : Command, ICommandServicesSource
{
    private readonly ICommandServices _commandServices;

    /// <summary>
    /// Creates the embeddable skills command group.
    /// </summary>
    /// <param name="serviceProvider">
    /// The provider built from a service collection that called
    /// <see cref="Extensions.ServiceCollectionExtensions.AddSkillsServices"/>. The five
    /// subcommands resolve their dependencies from this provider when invoked.
    /// </param>
    public SkillsCommand(IServiceProvider serviceProvider) : base("skills", "Manage AI agent skills")
    {
        _commandServices = new CommandServices(serviceProvider);
        SkillsSubcommands.AddTo(this);
    }

    ICommandServices ICommandServicesSource.CommandServices => _commandServices;
}
