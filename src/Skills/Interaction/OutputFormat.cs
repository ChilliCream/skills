namespace Skills.Interaction;

/// <summary>
/// The machine-readable output format a run can be switched into via <see
/// cref="IInteractionService.SetOutputFormat"/>. A single member today because skillz has exactly
/// one JSON-emitting command; a second format would add a member here rather than a new flag
/// threaded through every consumer.
/// </summary>
internal enum OutputFormat
{
    Json
}
