namespace Skills.Commands.List.Options;

internal sealed class GlobalOption : Option<bool>
{
    public const string OptionName = "--global";

    public GlobalOption() : base(OptionName, "-g")
    {
        Description = "List global skills";
    }
}
