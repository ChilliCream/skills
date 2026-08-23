using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Skills.Install;
using Skills.Locking;
using Skills.Tests.TestServices;
using Skills.Tests.Utils;
using Xunit;

namespace Skills.Tests.Commands;

[Collection(CommandTestCollection.Name)]
public class RemoveCommandSnapshotTests : IDisposable
{
    private readonly string _workspace;
    private readonly string _originalCwd;

    public RemoveCommandSnapshotTests()
    {
        _workspace = Path.Combine(Path.GetTempPath(), "skills-remove-snap-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workspace);
        _originalCwd = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_workspace);
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(_originalCwd);
        try
        {
            if (Directory.Exists(_workspace))
            {
                Directory.Delete(_workspace, recursive: true);
            }
        }
        catch
        {
            // best-effort
        }
    }

    private static void CreateSkill(string baseDir, string name)
    {
        var dir = Path.Combine(baseDir, name);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "SKILL.md"), $"---\nname: {name}\ndescription: x\n---\n");
    }

    private void ConfigureInstaller(TestInstaller installer)
    {
        installer.OnGetCanonicalSkillsDir = (_, cwd) => Path.Combine(cwd ?? _workspace, ".agents", "skills");
        installer.OnGetCanonicalPath = (name, _, cwd) => Path.Combine(cwd ?? _workspace, ".agents", "skills", name);
        installer.OnGetAgentBaseDir = (_, _, cwd) => Path.Combine(cwd ?? _workspace, ".agents", "skills");
        installer.OnGetInstallPath = (name, _, _, cwd) => Path.Combine(cwd ?? _workspace, ".agents", "skills", name);
    }

    private IServiceProvider BuildServices()
    {
        var services = CliTestHelper.CreateServiceProvider(workspace: _workspace, useRealFileStore: true);
        ConfigureInstaller((TestInstaller)services.GetRequiredService<ISkillInstaller>());
        return services;
    }

    [Fact]
    public async Task Remove_Help_Shows_Examples()
    {
        // Arrange
        var services = BuildServices();

        // Act
        var output = await CommandSnapshot.RunAsync(services, "remove", "--help");

        // Assert
        output.MatchInlineSnapshot(
            """
            $ skills remove --help

            Description:
              Remove installed skills

            Usage:
              Skills.Tests remove [<skills>...] [options]

            Arguments:
              <skills>  Skill names to remove

            Options:
              -g, --global         Remove from global installation
              -a, --agent <agent>  Target agent(s)
              -y, --yes            Skip prompts (non-interactive)
              --all                Remove all installed skills
              -?, -h, --help       Show help and usage information

            Example:
              skills remove my-skill --yes
              skills remove --all --yes
            """);
    }

    [Fact]
    public async Task Remove_With_No_Skills()
    {
        // Arrange
        var services = BuildServices();

        // Act
        var output = await CommandSnapshot.RunAsync(services, "remove", "--yes");

        // Assert
        output.MatchInlineSnapshot(
            """
            $ skills remove --yes

            No skills found to remove.
            """);
    }

    [Fact]
    public async Task Remove_Named_Skill()
    {
        // Arrange
        var canonical = Path.Combine(_workspace, ".agents", "skills");
        Directory.CreateDirectory(canonical);
        CreateSkill(canonical, "alpha");
        var services = BuildServices();
        var projectLock = (TestProjectLockFile)services.GetRequiredService<IProjectLockFile>();
        projectLock.OnRemoveEntry = (_, _) => true;

        // Act
        var output = await CommandSnapshot.RunAsync(services, "remove", "alpha", "--yes");

        // Assert
        output.MatchInlineSnapshot(
            """
            $ skills remove alpha --yes

            Successfully removed 1 skill(s)
            """);
    }

    [Fact]
    public async Task Remove_Named_Skill_No_Match()
    {
        // Arrange
        var canonical = Path.Combine(_workspace, ".agents", "skills");
        Directory.CreateDirectory(canonical);
        CreateSkill(canonical, "alpha");
        var services = BuildServices();

        // Act
        var output = await CommandSnapshot.RunAsync(services, "remove", "nope", "--yes");

        // Assert
        output.MatchInlineSnapshot(
            """
            $ skills remove nope --yes

            No matching skills found for: nope
            """);
    }

    [Fact]
    public async Task Remove_All()
    {
        // Arrange
        var canonical = Path.Combine(_workspace, ".agents", "skills");
        Directory.CreateDirectory(canonical);
        CreateSkill(canonical, "alpha");
        var services = BuildServices();
        var projectLock = (TestProjectLockFile)services.GetRequiredService<IProjectLockFile>();
        projectLock.OnRemoveEntry = (_, _) => true;

        // Act
        var output = await CommandSnapshot.RunAsync(services, "remove", "--all");

        // Assert
        output.MatchInlineSnapshot(
            """
            $ skills remove --all

            Successfully removed 1 skill(s)
            """);
    }

    [Fact]
    public async Task Remove_With_Invalid_Agent_Fails()
    {
        // Arrange
        var services = BuildServices();

        // Act
        var output = await CommandSnapshot.RunAsync(services, "remove", "alpha", "--agent", "bogus");

        // Assert
        output.MatchInlineSnapshot(
            """
            $ skills remove alpha --agent bogus
            # exit 1


            ┌─Invalid agents───────────────────────────────────────────────────────────────┐
            │ Invalid agents: bogus                                                        │
            │                                                                              │
            │ Valid agents: adal, aider-desk, amp, antigravity, augment, bob, claude-code, │
            │ cline, codearts-agent, codebuddy, codemaker, codestudio, codex,              │
            │ command-code, continue, cortex, crush, cursor, deepagents, devin, dexto,     │
            │ droid, firebender, forgecode, gemini-cli, github-copilot, goose,             │
            │ hermes-agent, iflow-cli, junie, kilo, kimi-cli, kiro-cli, kode, mcpjam,      │
            │ mistral-vibe, mux, neovate, openclaw, opencode, openhands, pi, pochi, qoder, │
            │ qwen-code, replit, roo, rovodev, tabnine-cli, trae, trae-cn, universal,      │
            │ warp, windsurf, zencoder                                                     │
            └──────────────────────────────────────────────────────────────────────────────┘
            """);
    }

    [Fact]
    public async Task Remove_No_Names_Non_Interactive_Reports_Nothing_Specified()
    {
        // Arrange
        var canonical = Path.Combine(_workspace, ".agents", "skills");
        Directory.CreateDirectory(canonical);
        CreateSkill(canonical, "alpha");
        var services = BuildServices();

        // Act
        var output = await CommandSnapshot.RunAsync(services, "remove", "--yes");

        // Assert
        output.MatchInlineSnapshot(
            """
            $ skills remove --yes

            No skills specified for removal.
            """);
    }

    [Fact]
    public async Task Remove_Interactive_Decline_Confirmation_Cancels()
    {
        // Arrange
        var canonical = Path.Combine(_workspace, ".agents", "skills");
        Directory.CreateDirectory(canonical);
        CreateSkill(canonical, "alpha");
        var services = BuildServices();
        var prompter = services.GetRequiredService<TestRemoveCommandPrompter>();
        prompter.OnConfirmRemoval = _ => false;

        // Act
        var output = await CommandSnapshot.RunAsync(services, "remove", "alpha");

        // Assert
        output.MatchInlineSnapshot(
            """
            $ skills remove alpha
            # exit 130

            Removal cancelled
            """);
    }

    [Fact]
    public async Task Remove_Reports_Failure_When_Lock_Update_Throws()
    {
        // Arrange
        var canonical = Path.Combine(_workspace, ".agents", "skills");
        Directory.CreateDirectory(canonical);
        CreateSkill(canonical, "alpha");
        var services = BuildServices();
        var projectLock = (TestProjectLockFile)services.GetRequiredService<IProjectLockFile>();
        projectLock.OnRemoveEntry = (_, _) => throw new InvalidOperationException("lock file is read-only");

        // Act
        var output = await CommandSnapshot.RunAsync(services, "remove", "alpha", "--yes");

        // Assert
        output.MatchInlineSnapshot(
            """
            $ skills remove alpha --yes
            # exit 1

            Failed to remove 1 skill(s)
              alpha: lock file is read-only
            """);
    }
}
