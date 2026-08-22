namespace Skills.Commands;

internal sealed class SkillsRootCommand : RootCommand
{
    public SkillsRootCommand(
        AddCommand add,
        RemoveCommand remove,
        ListCommand list,
        InitCommand init,
        UpdateCommand update) : base("Skills - AI agent skill manager")
    {
        Subcommands.Add(add);
        Subcommands.Add(remove);
        Subcommands.Add(list);
        Subcommands.Add(init);
        Subcommands.Add(update);
    }
}
