namespace Skills.Options;

internal sealed class OptionalOutputFormatOption : Option<string?>
{
    public const string OptionName = "--format";

    public OptionalOutputFormatOption() : base(OptionName)
    {
        Description = "Output format (text|json)";
        AcceptOnlyFromAmong("text", "json");
    }
}
