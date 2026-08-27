using Skills;
using Skills.Install;
using Skills.Interaction;
using Skills.Tests.TestServices;
using Xunit;

namespace Skills.Tests.Interaction;

public class BannerServiceTests
{
    private static BannerService CreateBannerService(TestInteractionService interaction)
    {
        var system = new FakeSystemEnvironment();
        var registry = new AgentRegistry(system);
        var agentEnvironment = new AgentEnvironment(registry, system);
        var context = new CliExecutionContext { CommandName = "skills" };
        return new BannerService(interaction, context, agentEnvironment);
    }

    [Fact]
    public void ShowBanner_Writes_When_Output_Format_Is_Unset()
    {
        var interaction = new TestInteractionService();
        var banner = CreateBannerService(interaction);

        banner.ShowBanner();

        Assert.NotEmpty(interaction.Output);
    }

    [Fact]
    public void ShowBanner_Writes_Nothing_When_Output_Format_Is_Json()
    {
        var interaction = new TestInteractionService();
        interaction.SetOutputFormat(OutputFormat.Json);
        var banner = CreateBannerService(interaction);

        banner.ShowBanner();

        Assert.Empty(interaction.Output);
    }

    [Fact]
    public void ShowLogo_Writes_Nothing_When_Output_Format_Is_Json()
    {
        var interaction = new TestInteractionService();
        interaction.SetOutputFormat(OutputFormat.Json);
        var banner = CreateBannerService(interaction);

        banner.ShowLogo();

        Assert.Empty(interaction.Output);
    }
}
