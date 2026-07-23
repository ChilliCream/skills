using Skills.Net;

namespace Skills.Tests.TestServices;

internal sealed class TestGitHubTokenProvider : IGitHubTokenProvider
{
    public Func<string?>? OnGetToken { get; set; }

    public Task<string?> FindTokenAsync(CancellationToken cancellationToken)
    {
        var result = OnGetToken?.Invoke();
        return Task.FromResult(result);
    }
}
