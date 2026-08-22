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
        CliTestHelper.SetCommandExecutionContext(provider);

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
