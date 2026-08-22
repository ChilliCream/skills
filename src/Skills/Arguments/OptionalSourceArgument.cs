namespace Skills.Arguments;

internal sealed class OptionalSourceArgument : Argument<string?>
{
    public const string ArgumentName = "source";

    public OptionalSourceArgument() : base(ArgumentName)
    {
        Description = "Source to fetch skills from (e.g., owner/repo, URL, local path)";
        Arity = ArgumentArity.ZeroOrOne;
    }
}
