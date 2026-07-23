using Microsoft.Extensions.DependencyInjection;
using Skills.Git;
using Skills.Install;
using Skills.Interaction;
using Skills.Locking;
using Skills.Net;
using Skills.Plugins;
using Skills.Skills;
using Skills.Sources;
using Skills.Tests.TestServices;
using Xunit;

namespace Skills.Tests.Utils;

public class CliTestHelperTests
{
    [Fact]
    public void CreateServiceProvider_Returns_Valid_Provider()
    {
        // Act
        var provider = CliTestHelper.CreateServiceProvider();
        var interaction = provider.GetRequiredService<IInteractionService>();

        // Assert
        Assert.NotNull(interaction);
        Assert.IsType<TestInteractionService>(interaction);
    }

    [Fact]
    public void CreateServiceProvider_Registers_All_TestDoubles()
    {
        // Act
        var provider = CliTestHelper.CreateServiceProvider();

        // Assert
        Assert.IsType<TestGitClient>(provider.GetRequiredService<IGitClient>());
        Assert.IsType<TestBlobClient>(provider.GetRequiredService<IBlobClient>());
        Assert.IsType<TestGitHubTokenProvider>(provider.GetRequiredService<IGitHubTokenProvider>());
        Assert.IsType<AgentEnvironment>(provider.GetRequiredService<AgentEnvironment>());
        Assert.IsType<TestInstaller>(provider.GetRequiredService<ISkillInstaller>());
        Assert.IsType<TestSkillDiscovery>(provider.GetRequiredService<ISkillDiscovery>());
        Assert.IsType<TestSourceParser>(provider.GetRequiredService<ISourceParser>());
        Assert.IsType<TestProjectLockFile>(provider.GetRequiredService<IProjectLockFile>());
        Assert.IsType<TestGlobalLockFile>(provider.GetRequiredService<IGlobalLockFile>());
    }

    [Fact]
    public void CreateServiceProvider_Registers_Real_Singletons()
    {
        // Act
        var provider = CliTestHelper.CreateServiceProvider();

        // Assert
        Assert.IsType<TestConsoleEnvironment>(provider.GetRequiredService<ConsoleEnvironment>());
        Assert.IsType<CliExecutionContext>(provider.GetRequiredService<CliExecutionContext>());
        Assert.IsType<AgentRegistry>(provider.GetRequiredService<AgentRegistry>());
    }

    [Fact]
    public void Configure_Callback_Can_Override_Registrations()
    {
        // Arrange
        var custom = new TestGitClient();

        // Act
        var provider = CliTestHelper.CreateServiceProvider(configure: services =>
        {
            services.AddSingleton<IGitClient>(custom);
        });

        // Assert
        Assert.Same(custom, provider.GetRequiredService<IGitClient>());
    }

    [Fact]
    public void Workspace_Argument_Is_Available()
    {
        // Act
        var provider = CliTestHelper.CreateServiceProvider(workspace: "/tmp/ws");
        var workspace = provider.GetRequiredService<TestWorkspace>();

        // Assert
        Assert.Equal("/tmp/ws", workspace.Path);
    }

    [Fact]
    public async Task TestInteractionService_Records_Output()
    {
        // Arrange
        var provider = CliTestHelper.CreateServiceProvider();
        var interaction = (TestInteractionService)provider.GetRequiredService<IInteractionService>();

        // Act
        interaction.WriteLine("hello");
        interaction.WriteError("oops");
        await interaction.StatusAsync("loading", () => Task.CompletedTask);

        // Assert
        Assert.Contains("hello", interaction.Output);
        Assert.Contains("ERROR: oops", interaction.Output);
        Assert.Contains("STATUS: loading", interaction.Output);
    }

    [Fact]
    public async Task TestInteractionService_Uses_Callback_For_Confirm()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var provider = CliTestHelper.CreateServiceProvider();
        var interaction = (TestInteractionService)provider.GetRequiredService<IInteractionService>();

        interaction.OnConfirm = (_, _) => true;

        // Act
        var confirm = await interaction.ConfirmAsync("ok?", defaultValue: false, ct);

        // Assert
        Assert.True(confirm);
    }

    [Fact]
    public async Task TestInteractionService_Returns_Default_When_No_Callback()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var provider = CliTestHelper.CreateServiceProvider();
        var interaction = (TestInteractionService)provider.GetRequiredService<IInteractionService>();

        // Act
        var confirm = await interaction.ConfirmAsync("ok?", defaultValue: true, cancellationToken: ct);

        // Assert
        Assert.True(confirm);
    }
}
