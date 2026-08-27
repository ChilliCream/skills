namespace Skills.Commands.Update.Arguments;

internal sealed class SkillsArgument : Argument<string[]>
{
    public const string ArgumentName = "skills";

    public SkillsArgument() : base(ArgumentName)
    {
        Description = "Optional skill names to update.";
        Arity = ArgumentArity.ZeroOrMore;
    }
}
