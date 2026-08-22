namespace Skills.Options;

internal sealed class FullDepthOption : Option<bool>
{
    public const string OptionName = "--full-depth";

    public FullDepthOption() : base(OptionName)
    {
        Description = "Full-depth clone";
    }
}
