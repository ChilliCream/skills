namespace Skills.Options;

internal sealed class YesOption : Option<bool>
{
    public const string OptionName = "--yes";

    public YesOption() : base(OptionName, "-y")
    {
        Description = "Skip prompts (non-interactive)";
    }
}
