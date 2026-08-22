namespace Skills.Commands.Remove.Options;

internal sealed class AllOption : Option<bool>
{
    public const string OptionName = "--all";

    public AllOption() : base(OptionName)
    {
        Description = "Remove all installed skills";
    }
}
