using Skills.Extensions;

namespace Skills.Options;

internal sealed class AgentOption : Option<string[]>
{
    public const string OptionName = "--agent";

    public AgentOption() : base(OptionName, "-a")
    {
        Description = "Target agent(s)";
        AllowMultipleArgumentsPerToken = true;
        this.NonEmptyStringsOnly();
    }
}
