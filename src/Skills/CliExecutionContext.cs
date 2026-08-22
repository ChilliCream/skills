namespace Skills;

internal sealed class CliExecutionContext
{
    public string CommandName { get; init; } = "skills";

    public Command? CurrentCommand { get; set; }

    public bool IsJsonOutput { get; set; }
}
