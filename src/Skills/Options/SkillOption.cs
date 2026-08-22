using Skills.Extensions;

namespace Skills.Options;

internal sealed class SkillOption : Option<string[]>
{
    public const string OptionName = "--skill";

    public SkillOption() : base(OptionName, "-s")
    {
        Description = "Skill name filter(s)";
        AllowMultipleArgumentsPerToken = true;
        this.NonEmptyStringsOnly();
    }
}
