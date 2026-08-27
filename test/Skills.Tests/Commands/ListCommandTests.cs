using Microsoft.Extensions.DependencyInjection;
using Skills.Commands;
using Skills.Install;
using Skills.Interaction;
using Skills.Tests.TestServices;
using Skills.Tests.Utils;
using Xunit;

namespace Skills.Tests.Commands;

[Collection(CommandTestCollection.Name)]
public class ListCommandTests : IDisposable
{
    private readonly string _workspace;
    private readonly string _originalCwd;
    private readonly TextWriter _originalOut;
    private readonly StringWriter _capturedOut;

    public ListCommandTests()
    {
        _workspace = Path.Combine(Path.GetTempPath(), "skills-list-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workspace);
        _originalCwd = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_workspace);
        _originalOut = Console.Out;
        _capturedOut = new StringWriter();
        Console.SetOut(_capturedOut);
    }

    public void Dispose()
    {
        Console.SetOut(_originalOut);
        _capturedOut.Dispose();
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
        File.WriteAllText(Path.Combine(dir, "SKILL.md"), $"---\nname: {name}\ndescription: a test\n---\n");
    }

    private void ConfigureInstaller(TestInstaller installer)
    {
        installer.OnGetCanonicalSkillsDir = (global, cwd) => Path.Combine(cwd ?? _workspace, ".agents", "skills");

        installer.OnGetAgentBaseDir = (agentType, global, cwd) =>
        {
            if (agentType == "claude-code")
            {
                return Path.Combine(cwd ?? _workspace, ".claude", "skills");
            }

            return Path.Combine(cwd ?? _workspace, ".agents", "skills");
        };
    }

    [Fact]
    public async Task List_With_No_Installed_Skills_Reports_Empty()
    {
        // Arrange
        var services = CliTestHelper.CreateServiceProvider(workspace: _workspace, useRealFileStore: true);
        var installer = (TestInstaller)services.GetRequiredService<ISkillInstaller>();
        ConfigureInstaller(installer);

        // Act
        var cmd = services.GetRequiredService<ListCommand>();
        var parseResult = cmd.Parse(Array.Empty<string>());
        var exitCode = await parseResult.InvokeAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(0, exitCode);
        var interaction = (TestInteractionService)services.GetRequiredService<IInteractionService>();
        Assert.Contains(interaction.Output, o => o.Contains("No project skills", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task List_Reports_Installed_Skills_From_Canonical_Dir()
    {
        // Arrange
        var canonical = Path.Combine(_workspace, ".agents", "skills");
        Directory.CreateDirectory(canonical);
        CreateSkill(canonical, "alpha");
        CreateSkill(canonical, "beta");

        var services = CliTestHelper.CreateServiceProvider(workspace: _workspace, useRealFileStore: true);
        var installer = (TestInstaller)services.GetRequiredService<ISkillInstaller>();
        ConfigureInstaller(installer);

        // Act
        var cmd = services.GetRequiredService<ListCommand>();
        var parseResult = cmd.Parse(Array.Empty<string>());
        var exitCode = await parseResult.InvokeAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(0, exitCode);
        var interaction = (TestInteractionService)services.GetRequiredService<IInteractionService>();
        var output = interaction.OutputText;
        Assert.Contains("alpha", output);
        Assert.Contains("beta", output);
    }

    [Fact]
    public async Task List_With_Json_Format_Writes_Json_To_Stdout()
    {
        // Arrange
        var canonical = Path.Combine(_workspace, ".agents", "skills");
        Directory.CreateDirectory(canonical);
        CreateSkill(canonical, "alpha");

        var services = CliTestHelper.CreateServiceProvider(workspace: _workspace, useRealFileStore: true);
        var installer = (TestInstaller)services.GetRequiredService<ISkillInstaller>();
        ConfigureInstaller(installer);

        // Act
        var cmd = services.GetRequiredService<ListCommand>();
        var parseResult = cmd.Parse(["--format", "json"]);
        var exitCode = await parseResult.InvokeAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(0, exitCode);
        var stdout = _capturedOut.ToString();
        Assert.Contains("\"name\"", stdout);
        Assert.Contains("alpha", stdout);
        Assert.Contains("\"scope\"", stdout);
    }

    [Fact]
    public async Task List_With_Output_Alias_Writes_Same_Json_As_Format()
    {
        // Arrange
        var canonical = Path.Combine(_workspace, ".agents", "skills");
        Directory.CreateDirectory(canonical);
        CreateSkill(canonical, "alpha");

        var formatServices = CliTestHelper.CreateServiceProvider(workspace: _workspace, useRealFileStore: true);
        ConfigureInstaller((TestInstaller)formatServices.GetRequiredService<ISkillInstaller>());
        var formatCmd = formatServices.GetRequiredService<ListCommand>();
        var formatParse = formatCmd.Parse(["--format", "json"]);
        Assert.Equal(0, await formatParse.InvokeAsync(cancellationToken: TestContext.Current.CancellationToken));
        var formatStdout = _capturedOut.ToString();

        _capturedOut.GetStringBuilder().Clear();

        var jsonFlagServices = CliTestHelper.CreateServiceProvider(workspace: _workspace, useRealFileStore: true);
        ConfigureInstaller((TestInstaller)jsonFlagServices.GetRequiredService<ISkillInstaller>());
        var jsonFlagCmd = jsonFlagServices.GetRequiredService<ListCommand>();
        var jsonFlagParse = jsonFlagCmd.Parse(["--json"]);
        Assert.Equal(0, await jsonFlagParse.InvokeAsync(cancellationToken: TestContext.Current.CancellationToken));
        var jsonFlagStdout = _capturedOut.ToString();

        _capturedOut.GetStringBuilder().Clear();

        var outputServices = CliTestHelper.CreateServiceProvider(workspace: _workspace, useRealFileStore: true);
        ConfigureInstaller((TestInstaller)outputServices.GetRequiredService<ISkillInstaller>());
        var outputCmd = outputServices.GetRequiredService<ListCommand>();
        var outputParse = outputCmd.Parse(["--output", "json"]);

        // Act
        var outputExitCode = await outputParse.InvokeAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(0, outputExitCode);
        var outputStdout = _capturedOut.ToString();
        Assert.Equal(formatStdout, outputStdout);
        Assert.Equal(jsonFlagStdout, outputStdout);
    }

    [Fact]
    public async Task List_With_Unknown_Output_Value_Fails_Parsing_Like_Format()
    {
        // Arrange
        var services = CliTestHelper.CreateServiceProvider(workspace: _workspace, useRealFileStore: true);
        var installer = (TestInstaller)services.GetRequiredService<ISkillInstaller>();
        ConfigureInstaller(installer);

        // Act
        var cmd = services.GetRequiredService<ListCommand>();
        var parseResult = cmd.Parse(["--output", "josn"]);
        var exitCode = await parseResult.InvokeAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, exitCode);
        Assert.Contains(parseResult.Errors, e => e.Message.Contains("josn", StringComparison.Ordinal));
    }

    [Fact]
    public async Task List_With_Unknown_Format_Fails_Parsing()
    {
        // Arrange
        var services = CliTestHelper.CreateServiceProvider(workspace: _workspace, useRealFileStore: true);
        var installer = (TestInstaller)services.GetRequiredService<ISkillInstaller>();
        ConfigureInstaller(installer);

        // Act
        var cmd = services.GetRequiredService<ListCommand>();
        var parseResult = cmd.Parse(["--format", "josn"]);
        var exitCode = await parseResult.InvokeAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, exitCode);
        Assert.Contains(parseResult.Errors, e => e.Message.Contains("josn", StringComparison.Ordinal));
    }

    [Fact]
    public async Task List_With_Invalid_Agent_Reports_Titled_Hint()
    {
        // Arrange
        var services = CliTestHelper.CreateServiceProvider(workspace: _workspace, useRealFileStore: true);
        var installer = (TestInstaller)services.GetRequiredService<ISkillInstaller>();
        ConfigureInstaller(installer);

        // Act
        var cmd = services.GetRequiredService<ListCommand>();
        var parseResult = cmd.Parse(["--agent", "bogus"]);
        var exitCode = await parseResult.InvokeAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, exitCode);
        var interaction = (TestInteractionService)services.GetRequiredService<IInteractionService>();
        Assert.Contains(
            interaction.Output,
            line => line.Contains("Invalid agents", StringComparison.Ordinal)
                && line.Contains("bogus", StringComparison.Ordinal));
        Assert.Contains(
            interaction.Output,
            line => line.Contains("TIP:", StringComparison.Ordinal)
                && line.Contains("Valid agents", StringComparison.Ordinal));
    }
}
