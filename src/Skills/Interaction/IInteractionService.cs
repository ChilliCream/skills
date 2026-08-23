using System.Collections.Immutable;
using Spectre.Console.Rendering;

namespace Skills.Interaction;

/// <summary>
/// The single seam for all console output and interactive prompts, so commands never write to
/// the terminal directly. This keeps rendering consistent and lets tests capture or stub interaction.
/// </summary>
internal interface IInteractionService
{
    /// <summary>
    /// True while the run is rendering for a person (the default). False once <see
    /// cref="SetOutputFormat"/> has switched the run to a machine-readable format, at which point
    /// the write methods below drop their output instead of mixing it into that format.
    /// </summary>
    bool IsHumanReadable { get; }

    /// <summary>
    /// Sets the run's output format: a machine-readable format, or <see langword="null"/> for
    /// human-readable. Callers resolve this from the current invocation's own parse result on
    /// every command, not just once, so a format set for one invocation of a shared, long-lived
    /// service instance never leaks into the next.
    /// </summary>
    void SetOutputFormat(OutputFormat? format);

    void WriteLine(string text = "");

    void WriteMarkupLine(string markup);

    void WriteRenderable(IRenderable renderable);

    void WriteError(string message);

    void WriteErrorPanel(string title, string message, string? tip = null);

    void WriteWarning(string message);

    void WriteSuccess(string message);

    void WriteDim(string text);

    Task<T> StatusAsync<T>(string status, Func<Task<T>> action);

    Task StatusAsync(string status, Func<Task> action);

    Task<bool> ConfirmAsync(string message, bool defaultValue, CancellationToken cancellationToken);

    Task<T> SelectAsync<T>(
        string message,
        IEnumerable<(string Label, T Value)> choices,
        CancellationToken cancellationToken)
        where T : notnull;

    Task<ImmutableArray<T>> MultiSelectAsync<T>(
        string message,
        IEnumerable<(string Label, T Value)> choices,
        CancellationToken cancellationToken)
        where T : notnull;

    Task<ImmutableArray<T>> MultiSelectAsync<T>(
        string message,
        IEnumerable<(string Label, T Value)> choices,
        IEnumerable<T> preSelected,
        CancellationToken cancellationToken)
        where T : notnull;

    /// <summary>
    /// Presents a multi-select where choices are grouped under headers. Items sharing a
    /// <paramref name="groupHeader"/> render under that header (in first-seen order), so the caller
    /// orders <paramref name="choices"/> up front to control both group and within-group order.
    /// </summary>
    Task<ImmutableArray<T>> MultiSelectGroupedAsync<T>(
        string message,
        IEnumerable<T> choices,
        Func<T, string> groupHeader,
        Func<T, string> label,
        CancellationToken cancellationToken)
        where T : notnull;

    Task<ImmutableArray<T>> SearchableMultiSelectAsync<T>(
        string title,
        IReadOnlyList<SearchableItem<T>> items,
        IEnumerable<T> preSelected,
        CancellationToken cancellationToken)
        where T : notnull;
}
