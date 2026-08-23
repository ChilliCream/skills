using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Skills.Install;
using Skills.Tests.TestServices;
using Skills.Tests.Utils;
using Xunit;

namespace Skills.Tests.Commands;

[Collection(CommandTestCollection.Name)]
public class ListCommandSnapshotTests : IDisposable
{
    private readonly string _workspace;
    private readonly string _originalCwd;

    public ListCommandSnapshotTests()
    {
        _workspace = Path.Combine(Path.GetTempPath(), "skills-list-snap-" + Guid.NewGuid().ToString("N"));
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

    private IServiceProvider BuildServices()
    {
        var services = CliTestHelper.CreateServiceProvider(workspace: _workspace, useRealFileStore: true);
        var installer = (TestInstaller)services.GetRequiredService<ISkillInstaller>();
        installer.OnGetCanonicalSkillsDir = (_, cwd) => Path.Combine(cwd ?? _workspace, ".agents", "skills");
        installer.OnGetAgentBaseDir = (agentType, _, cwd) => agentType == "claude-code"
            ? Path.Combine(cwd ?? _workspace, ".claude", "skills")
            : Path.Combine(cwd ?? _workspace, ".agents", "skills");
        return services;
    }

    private static void CreateSkill(string baseDir, string name)
    {
        var dir = Path.Combine(baseDir, name);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "SKILL.md"), $"---\nname: {name}\ndescription: a test\n---\n");
    }

    [Fact]
    public async Task List_With_No_Skills()
    {
        // Arrange
        var services = BuildServices();

        // Act
        var output = await CommandSnapshot.RunAsync(services, "list");

        // Assert
        output.MatchInlineSnapshot(
            """
            $ skills list

            No project skills found.
            Try listing global skills with -g
            """);
    }

    [Fact]
    public async Task List_With_Installed_Skill()
    {
        // Arrange
        var canonical = Path.Combine(_workspace, ".agents", "skills");
        Directory.CreateDirectory(canonical);
        CreateSkill(canonical, "alpha");
        var services = BuildServices();

        // Act
        var output = await CommandSnapshot.RunAsync(services, "list");

        // Assert
        output.MatchInlineSnapshot(
            """
            $ skills list

            Project Skills

            Skill  Path                    Agents
            alpha  ./.agents/skills/alpha  not linked
            """);
    }

    [Fact]
    public async Task List_As_Json()
    {
        // Arrange
        var canonical = Path.Combine(_workspace, ".agents", "skills");
        Directory.CreateDirectory(canonical);
        CreateSkill(canonical, "alpha");
        var services = BuildServices();

        // Act
        var output = await CommandSnapshot.RunAsync(services, "list", "--json");

        // Assert
        output.MatchInlineSnapshot(
            """
            $ skills list --json

            [
              {
                "name": "alpha",
                "path": "<cwd>/.agents/skills/alpha",
                "scope": "project",
                "agents": []
              }
            ]
            """);
    }

    [Fact]
    public async Task List_Help_Shows_Examples()
    {
        // Arrange
        var services = BuildServices();

        // Act
        var output = await CommandSnapshot.RunAsync(services, "list", "--help");

        // Assert
        output.MatchInlineSnapshot(
            """
            $ skills list --help

            Description:
              List installed skills

            Usage:
              Skills.Tests list [options]

            Options:
              -g, --global         List global skills
              -a, --agent <agent>  Target agent(s)
              --format <format>    Output format (text|json)
              --json               Output as JSON (alias for --format json)
              -?, -h, --help       Show help and usage information

            Example:
              skills list
              skills list -g --json
            """);
    }

    [Fact]
    public async Task List_With_Invalid_Agent_Fails()
    {
        // Arrange
        var services = BuildServices();

        // Act
        var output = await CommandSnapshot.RunAsync(services, "list", "--agent", "bogus");

        // Assert
        output.MatchInlineSnapshot(
            """
            $ skills list --agent bogus
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
    public async Task List_Global_With_No_Skills()
    {
        // Arrange
        var services = BuildServices();

        // Act
        var output = await CommandSnapshot.RunAsync(services, "list", "--global");

        // Assert
        output.MatchInlineSnapshot(
            """
            $ skills list --global

            No global skills found.
            """);
    }
}
