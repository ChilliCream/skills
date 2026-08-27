namespace Skills.Options;

internal sealed class ProjectOption : Option<bool>
{
    public const string OptionName = "--project";

    public ProjectOption() : base(OptionName, "-p")
    {
        Description = "Update project skills only.";
    }
}
