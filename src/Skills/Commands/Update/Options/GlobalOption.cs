namespace Skills.Commands.Update.Options;

internal sealed class GlobalOption : Option<bool>
{
    public const string OptionName = "--global";

    public GlobalOption() : base(OptionName, "-g")
    {
        Description = "Update global skills only.";
    }
}
