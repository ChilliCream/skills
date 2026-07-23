using System.Collections.Immutable;
using Skills.Commands;

namespace Skills.Tests.TestServices;

internal sealed class TestRemoveCommandPrompter : IRemoveCommandPrompter
{
    public Func<IReadOnlyList<string>, IReadOnlyList<string>>? OnSelectSkills { get; set; }

    public Func<IReadOnlyList<string>, bool>? OnConfirmRemoval { get; set; }

    public Task<ImmutableArray<string>> SelectSkillsAsync(
        ImmutableArray<string> installed,
        CancellationToken cancellationToken)
    {
        var result = OnSelectSkills is not null ? OnSelectSkills(installed) : installed;
        return Task.FromResult<ImmutableArray<string>>([.. result]);
    }

    public Task<bool> ConfirmRemovalAsync(ImmutableArray<string> skills, CancellationToken cancellationToken)
    {
        var result = OnConfirmRemoval is null || OnConfirmRemoval(skills);
        return Task.FromResult(result);
    }
}
