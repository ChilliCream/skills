using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Skills.Commands;
using Skills.Install;
using Skills.Interaction;
using Skills.Tests.TestServices;
using Skills.Tests.Utils;
using Xunit;

namespace Skills.Tests.Commands;

/// <summary>
/// Proves the embeddable <see cref="SkillsCommand"/> behaves the same as the standalone
/// <c>SkillsRootCommand</c> once composed under a host's own root.
/// </summary>
[Collection(CommandTestCollection.Name)]
public class SkillsCommandEmbeddingTests : IDisposable
{
    private readonly string _originalCwd;

    public SkillsCommandEmbeddingTests()
    {
        _originalCwd = Directory.GetCurrentDirectory();
    }

    public void Dispose() => Directory.SetCurrentDirectory(_originalCwd);

    [Fact]
    public async Task Embedded_List_Produces_The_Same_Output_As_Standalone()
    {
        var ct = TestContext.Current.CancellationToken;

        var standaloneServices = CliTestHelper.CreateServiceProvider();
        var standaloneInteraction = (TestInteractionService)standaloneServices.GetRequiredService<IInteractionService>();
        var standaloneExitCode = await standaloneServices.GetRequiredService<SkillsRootCommand>()
            .Parse(["list"])
            .InvokeAsync(cancellationToken: ct);

        var embeddedServices = CliTestHelper.CreateServiceProvider();
        var embeddedInteraction = (TestInteractionService)embeddedServices.GetRequiredService<IInteractionService>();
        var hostRoot = new RootCommand("host");
        hostRoot.Subcommands.Add(new SkillsCommand(embeddedServices));
        var embeddedExitCode = await hostRoot.Parse(["skills", "list"]).InvokeAsync(cancellationToken: ct);

        Assert.Equal(standaloneExitCode, embeddedExitCode);
        Assert.Equal(standaloneInteraction.OutputText, embeddedInteraction.OutputText);
    }

    [Fact]
    public async Task Embedded_Command_Uses_The_Provider_Passed_To_Its_Constructor()
    {
        var services = CliTestHelper.CreateServiceProvider();
        var installer = (TestInstaller)services.GetRequiredService<ISkillInstaller>();
        installer.OnGetCanonicalSkillsDir = (_, cwd) => Path.Combine(cwd ?? "/workspace", ".agents", "skills");
        installer.OnGetAgentBaseDir = (_, _, cwd) => Path.Combine(cwd ?? "/workspace", ".agents", "skills");
        var interaction = (TestInteractionService)services.GetRequiredService<IInteractionService>();

        var otherServices = CliTestHelper.CreateServiceProvider();
        var otherInteraction = (TestInteractionService)otherServices.GetRequiredService<IInteractionService>();

        var hostRoot = new RootCommand("host");
        hostRoot.Subcommands.Add(new SkillsCommand(services));

        var exitCode = await hostRoot.Parse(["skills", "list"])
            .InvokeAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(ExitCodeConstants.Success, exitCode);
        Assert.Contains("No project skills found.", interaction.Output);
        Assert.Empty(otherInteraction.Output);
    }

    [Fact]
    public async Task Embedded_Init_Produces_The_Same_Output_As_Standalone()
    {
        var ct = TestContext.Current.CancellationToken;

        var standaloneWorkspace = Path.Combine(Path.GetTempPath(), "skills-embed-standalone-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(standaloneWorkspace);
        Directory.SetCurrentDirectory(standaloneWorkspace);
        var standaloneServices = CliTestHelper.CreateServiceProvider(workspace: standaloneWorkspace, useRealFileStore: true);
        var standaloneInteraction = (TestInteractionService)standaloneServices.GetRequiredService<IInteractionService>();
        var standaloneExitCode = await standaloneServices.GetRequiredService<SkillsRootCommand>()
            .Parse(["init", "x"])
            .InvokeAsync(cancellationToken: ct);

        var embeddedWorkspace = Path.Combine(Path.GetTempPath(), "skills-embed-embedded-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(embeddedWorkspace);
        Directory.SetCurrentDirectory(embeddedWorkspace);
        var embeddedServices = CliTestHelper.CreateServiceProvider(workspace: embeddedWorkspace, useRealFileStore: true);
        var embeddedInteraction = (TestInteractionService)embeddedServices.GetRequiredService<IInteractionService>();
        var hostRoot = new RootCommand("host");
        hostRoot.Subcommands.Add(new SkillsCommand(embeddedServices));
        var embeddedExitCode = await hostRoot.Parse(["skills", "init", "x"]).InvokeAsync(cancellationToken: ct);

        try
        {
            Assert.Equal(standaloneExitCode, embeddedExitCode);
            Assert.Equal(standaloneInteraction.OutputText, embeddedInteraction.OutputText);
            Assert.True(File.Exists(Path.Combine(standaloneWorkspace, "x", "SKILL.md")));
            Assert.True(File.Exists(Path.Combine(embeddedWorkspace, "x", "SKILL.md")));
        }
        finally
        {
            TryDelete(standaloneWorkspace);
            TryDelete(embeddedWorkspace);
        }
    }

    [Fact]
    public async Task Json_Output_Format_Does_Not_Stick_Across_Invocations_On_A_Shared_Provider()
    {
        // Arrange: one provider, one root command - exactly what an embedding host reuses across
        // repeated invocations, since IInteractionService is registered as a singleton.
        var ct = TestContext.Current.CancellationToken;
        var services = CliTestHelper.CreateServiceProvider();
        var installer = (TestInstaller)services.GetRequiredService<ISkillInstaller>();
        installer.OnGetCanonicalSkillsDir = (_, cwd) => Path.Combine(cwd ?? "/workspace", ".agents", "skills");
        installer.OnGetAgentBaseDir = (agentType, _, cwd) => Path.Combine(cwd ?? "/workspace", ".agents", "skills");
        var interaction = (TestInteractionService)services.GetRequiredService<IInteractionService>();
        var root = services.GetRequiredService<SkillsRootCommand>();

        // Act: a --json invocation first, then a bare one on the same provider.
        var jsonExitCode = await root.Parse(["list", "--json"]).InvokeAsync(cancellationToken: ct);
        var jsonModeDuringFirstRun = !interaction.IsHumanReadable;

        var plainExitCode = await root.Parse(["list"]).InvokeAsync(cancellationToken: ct);

        // Assert: the first run rendered machine-readable, but it must not leak into the second -
        // the second renders human output rather than staying stuck in JSON mode.
        Assert.Equal(0, jsonExitCode);
        Assert.True(jsonModeDuringFirstRun);
        Assert.Equal(0, plainExitCode);
        Assert.True(interaction.IsHumanReadable);
        Assert.Contains("No project skills found.", interaction.Output);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // best-effort
        }
    }
}
