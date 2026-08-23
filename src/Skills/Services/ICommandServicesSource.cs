namespace Skills;

/// <summary>
/// Implemented by the composition roots (<c>SkillsCommand</c> and the internal
/// <c>SkillsRootCommand</c>) that carry the <see cref="ICommandServices"/> a subcommand
/// resolves at execution time. <see cref="CommandServicesResolver"/> walks up from the invoked
/// command to the nearest ancestor implementing this interface instead of reading ambient state,
/// so there is exactly one place a host must get right: constructing one of those two types with
/// a populated <see cref="IServiceProvider"/>.
/// </summary>
internal interface ICommandServicesSource
{
    ICommandServices CommandServices { get; }
}
