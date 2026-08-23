using Microsoft.Extensions.DependencyInjection;
using Skills.Commands;
using Skills.Extensions;
using Skills.Interaction;
using Skills.Tests.TestServices;
using Skills.Tests.Utils;
using Xunit;

namespace Skills.Tests;

public class RootCommandExtensionsTests
{
    [Fact]
    public void StripBareTerminators_Removes_Terminator_Between_Options_And_Positionals()
    {
        // Act & Assert
        Assert.Equal(
            ["add", "--agent", "codex", "owner/repo"],
            RootCommandExtensions.StripBareTerminators(["add", "--agent", "codex", "--", "owner/repo"]));
    }

    [Fact]
    public void StripBareTerminators_Removes_Terminator_Before_OptionLike_Value()
    {
        // Act & Assert
        Assert.Equal(
            ["add", "--upload-pack=sh"],
            RootCommandExtensions.StripBareTerminators(["add", "--", "--upload-pack=sh"]));
    }

    [Fact]
    public void StripBareTerminators_Removes_Multiple_Terminators()
    {
        // Act & Assert
        Assert.Equal(
            ["add", "owner/repo"],
            RootCommandExtensions.StripBareTerminators(["--", "add", "--", "owner/repo", "--"]));
    }

    [Fact]
    public async Task ExecuteAsync_With_No_Args_Shows_Banner_And_Returns_Success()
    {
        var ct = TestContext.Current.CancellationToken;
        var services = CliTestHelper.CreateServiceProvider(configure: s => s.AddSingleton<BannerService>());
        var interaction = (TestInteractionService)services.GetRequiredService<IInteractionService>();
        var root = services.GetRequiredService<SkillsRootCommand>();

        var exitCode = await root.ExecuteAsync([], services, invocationConfiguration: null, ct);

        Assert.Equal(ExitCodeConstants.Success, exitCode);
        Assert.NotEmpty(interaction.Output);
    }

    [Fact]
    public async Task ExecuteAsync_With_Parse_Error_Skips_Curated_Path_And_Invokes_ParseResult_Directly()
    {
        // An empty --agent value fails that option's validator, which System.CommandLine records
        // in ParseResult.Errors synchronously during Parse (unlike a missing required argument,
        // which only surfaces once the command action runs). A populated command identified as
        // "add" is exactly the case RootCommandExtensions.ExecuteAsync's early-return branch must
        // catch ahead of its "add"/"init" logo check: the logo must not render for a run that never
        // reaches a command action.
        var ct = TestContext.Current.CancellationToken;
        var services = CliTestHelper.CreateServiceProvider(configure: s => s.AddSingleton<BannerService>());
        var interaction = (TestInteractionService)services.GetRequiredService<IInteractionService>();
        var root = services.GetRequiredService<SkillsRootCommand>();

        var originalOut = Console.Out;
        var originalError = Console.Error;
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        Console.SetOut(stdout);
        Console.SetError(stderr);

        int exitCode;
        try
        {
            exitCode = await root.ExecuteAsync(
                ["add", "owner/repo", "--agent", ""],
                services,
                invocationConfiguration: null,
                ct);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }

        Assert.NotEqual(ExitCodeConstants.Success, exitCode);
        Assert.Empty(interaction.Output);
        Assert.Contains("--agent", stdout.ToString() + stderr.ToString(), StringComparison.Ordinal);
    }
}
