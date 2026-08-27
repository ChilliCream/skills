namespace Skills.Options;

internal sealed class JsonOption : Option<bool>
{
    public const string OptionName = "--json";

    public JsonOption() : base(OptionName)
    {
        Description = "Output as JSON (alias for --format json)";
    }
}
