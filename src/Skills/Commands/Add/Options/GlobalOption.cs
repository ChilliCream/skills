namespace Skills.Commands.Add.Options;

internal sealed class GlobalOption : Option<bool>
{
    public const string OptionName = "--global";

    public GlobalOption() : base(OptionName, "-g")
    {
        Description = "Install globally";
    }
}
