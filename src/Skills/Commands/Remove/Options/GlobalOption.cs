namespace Skills.Commands.Remove.Options;

internal sealed class GlobalOption : Option<bool>
{
    public const string OptionName = "--global";

    public GlobalOption() : base(OptionName, "-g")
    {
        Description = "Remove from global installation";
    }
}
