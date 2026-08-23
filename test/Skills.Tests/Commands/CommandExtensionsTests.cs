using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Skills.Extensions;
using Skills.Interaction;
using Skills.Tests.TestServices;
using Skills.Tests.Utils;
using Xunit;

namespace Skills.Tests.Commands;

[Collection(CommandTestCollection.Name)]
public class CommandExtensionsTests
{
    private static (Command Command, TestInteractionService Interaction) CreateCommand(Action action)
    {
        var provider = CliTestHelper.CreateServiceProvider();
        var interaction = (TestInteractionService)provider.GetRequiredService<IInteractionService>();

        var command = new Command("probe");
        command.SetActionWithExceptionHandling((_, _, _) =>
        {
            action();
            return Task.FromResult(ExitCodeConstants.Success);
        });

        return (command, interaction);
    }

    private static Task<int> InvokeAsync(Command command, CancellationToken cancellationToken)
        => command.Parse([]).InvokeAsync(cancellationToken: cancellationToken);

    [Fact]
    public async Task ExitException_With_Message_Writes_Error_And_Returns_Failure()
    {
        var ct = TestContext.Current.CancellationToken;
        var (command, interaction) = CreateCommand(() => throw new ExitException("boom"));

        var exitCode = await InvokeAsync(command, ct);

        Assert.Equal(ExitCodeConstants.Failure, exitCode);
        Assert.Contains("ERROR: boom", interaction.Output);
    }

    [Fact]
    public async Task ExitException_With_Empty_Message_Writes_Nothing_And_Returns_Failure()
    {
        var ct = TestContext.Current.CancellationToken;
        var (command, interaction) = CreateCommand(() => throw new ExitException());

        var exitCode = await InvokeAsync(command, ct);

        Assert.Equal(ExitCodeConstants.Failure, exitCode);
        Assert.Empty(interaction.Output);
    }

    [Fact]
    public async Task CliException_With_Title_And_Hint_Writes_Panel_And_Returns_Its_ExitCode()
    {
        var ct = TestContext.Current.CancellationToken;
        var (command, interaction) = CreateCommand(() => throw new CliException(
            42,
            "something went wrong",
            title: "Operation failed",
            hint: "try again"));

        var exitCode = await InvokeAsync(command, ct);

        Assert.Equal(42, exitCode);
        Assert.Contains("ERROR: Operation failed: something went wrong", interaction.Output);
        Assert.Contains("TIP: try again", interaction.Output);
    }

    [Fact]
    public async Task CliException_Without_Title_Writes_Message_Only_And_Drops_Hint()
    {
        // A title-less CliException goes through WriteError, which has no hint parameter - so a
        // hint set alongside a null title is dropped rather than rendered anywhere. Every current
        // call site that sets a hint also sets a title, so this is a latent, not live, gap; pinned
        // here so a future title-less exception with a hint gets a deliberate decision instead of
        // silently losing it.
        var ct = TestContext.Current.CancellationToken;
        var (command, interaction) = CreateCommand(() => throw new CliException(
            7,
            "something went wrong",
            hint: "try again"));

        var exitCode = await InvokeAsync(command, ct);

        Assert.Equal(7, exitCode);
        Assert.Contains("ERROR: something went wrong", interaction.Output);
        Assert.DoesNotContain(interaction.Output, line => line.Contains("try again", StringComparison.Ordinal));
    }

    [Fact]
    public async Task OperationCanceledException_Returns_Cancelled_ExitCode()
    {
        var ct = TestContext.Current.CancellationToken;
        var (command, interaction) = CreateCommand(() => throw new OperationCanceledException());

        var exitCode = await InvokeAsync(command, ct);

        Assert.Equal(ExitCodeConstants.Cancelled, exitCode);
        Assert.Empty(interaction.Output);
    }

    [Fact]
    public async Task Unexpected_Exception_Writes_Generic_Error_And_Returns_Failure()
    {
        var ct = TestContext.Current.CancellationToken;
        var (command, interaction) = CreateCommand(() => throw new InvalidOperationException("kaboom"));

        var exitCode = await InvokeAsync(command, ct);

        Assert.Equal(ExitCodeConstants.Failure, exitCode);
        Assert.Contains("ERROR: There was an unexpected error: kaboom", interaction.Output);
    }
}
