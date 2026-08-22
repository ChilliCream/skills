namespace Skills.Commands.Remove.Arguments;

internal sealed class SkillsArgument : Argument<string[]>
{
    public const string ArgumentName = "skills";

    public SkillsArgument() : base(ArgumentName)
    {
        Description = "Skill names to remove";
        Arity = ArgumentArity.ZeroOrMore;
    }
}
