namespace Skills.Cli;

/// <summary>
/// The process entry point for the standalone <c>skills</c> executable. Hosts embedding the
/// skills commands (see docs/embedding.md) do not use this type; they compose
/// <c>SkillsCommand</c> under their own root and run their own pipeline instead.
/// </summary>
internal static class Program
{
    public static Task<int> Main(string[] args) => global::Skills.Program.RunAsync(args, toolCommandName: null);
}
