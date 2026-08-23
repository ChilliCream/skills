using CookieCrumble;
using Skills.Tests.Utils;
using Xunit;

namespace Skills.Tests.Commands;

[Collection(CommandTestCollection.Name)]
public class InitCommandSnapshotTests : IDisposable
{
    private readonly string _workspace;
    private readonly string _originalCwd;

    public InitCommandSnapshotTests()
    {
        _workspace = Path.Combine(Path.GetTempPath(), "skills-init-snap-" + Guid.NewGuid().ToString("N"));
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

    [Fact]
    public async Task Init_With_Name_Creates_Skill()
    {
        // Arrange
        var services = CliTestHelper.CreateServiceProvider(workspace: _workspace, useRealFileStore: true);

        // Act
        var output = await CommandSnapshot.RunAsync(services, "init", "my-skill");

        // Assert
        output.MatchInlineSnapshot(
            """
            $ skills init my-skill

            Initialized skill: my-skill

            Created:
              my-skill/SKILL.md

            Next steps:
              1. Edit my-skill/SKILL.md to define your skill instructions
              2. Update the name and description in the frontmatter

            Publishing:
              GitHub: Push to a repo, then skills add <owner>/<repo>
              URL:    Host the file, then skills add https://example.com/my-skill/SKILL.md
            """);
    }

    [Fact]
    public async Task Init_Help_Shows_Examples()
    {
        // Arrange
        var services = CliTestHelper.CreateServiceProvider(workspace: _workspace, useRealFileStore: true);

        // Act
        var output = await CommandSnapshot.RunAsync(services, "init", "--help");

        // Assert
        output.MatchInlineSnapshot(
            """
            $ skills init --help

            Description:
              Initialize a new skill (creates SKILL.md)

            Usage:
              Skills.Tests init [<name>] [options]

            Arguments:
              <name>  Skill name (creates <name>/SKILL.md). Defaults to current directory.

            Options:
              -?, -h, --help  Show help and usage information

            Example:
              skills init
              skills init my-skill
            """);
    }

    [Fact]
    public async Task Init_When_Skill_Already_Exists_Warns()
    {
        // Arrange
        var skillDir = Path.Combine(_workspace, "my-skill");
        Directory.CreateDirectory(skillDir);
        await File.WriteAllTextAsync(
            Path.Combine(skillDir, "SKILL.md"),
            "existing content",
            TestContext.Current.CancellationToken);
        var services = CliTestHelper.CreateServiceProvider(workspace: _workspace, useRealFileStore: true);

        // Act
        var output = await CommandSnapshot.RunAsync(services, "init", "my-skill");

        // Assert
        output.MatchInlineSnapshot(
            """
            $ skills init my-skill

            Skill already exists at my-skill/SKILL.md
            """);
    }
}
