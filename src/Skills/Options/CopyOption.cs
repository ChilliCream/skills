namespace Skills.Options;

internal sealed class CopyOption : Option<bool>
{
    public const string OptionName = "--copy";

    public CopyOption() : base(OptionName)
    {
        Description = "Copy instead of symlinking";
    }
}
