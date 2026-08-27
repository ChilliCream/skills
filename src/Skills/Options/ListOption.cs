namespace Skills.Options;

internal sealed class ListOption : Option<bool>
{
    public const string OptionName = "--list";

    public ListOption() : base(OptionName, "-l")
    {
        Description = "List available skills without installing";
    }
}
