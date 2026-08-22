namespace Skills.Arguments;

internal sealed class OptionalSkillNameArgument : Argument<string?>
{
    public const string ArgumentName = "name";

    public OptionalSkillNameArgument() : base(ArgumentName)
    {
        Description = "Skill name (creates <name>/SKILL.md). Defaults to current directory.";
        Arity = ArgumentArity.ZeroOrOne;
    }
}
