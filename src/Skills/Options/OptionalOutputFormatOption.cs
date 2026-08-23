namespace Skills.Options;

internal sealed class OptionalOutputFormatOption : Option<string?>
{
    public const string OptionName = "--format";
    public const string OutputAliasName = "--output";

    public OptionalOutputFormatOption() : base(OptionName)
    {
        Description = "Output format (text|json)";
        Aliases.Add(OutputAliasName);
        AcceptOnlyFromAmong("text", "json");
    }
}
