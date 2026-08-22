namespace Skills.Commands.Add.Options;

internal sealed class AllOption : Option<bool>
{
    public const string OptionName = "--all";

    public AllOption() : base(OptionName)
    {
        Description = "Install all skills to all agents";
    }
}
