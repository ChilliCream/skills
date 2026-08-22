using Skills.Commands;
using Skills.Git;
using Skills.Install;
using Skills.Interaction;
using Skills.Locking;
using Skills.Net;
using Skills.Plugins;
using Skills.Skills;
using Skills.Sources;
using Skills.Sources.Providers;
using Skills.Utils;

namespace Skills.Extensions;

/// <summary>
/// Registers every service the Skills CLI needs to resolve commands and their dependencies.
/// </summary>
internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSkillsServices(this IServiceCollection services, string toolCommandName)
    {
        services.AddSingleton(AnsiConsole.Console);
        services.AddSingleton<ConsoleEnvironment>();
        services.AddSingleton(new CliExecutionContext { CommandName = toolCommandName });
        services.AddSingleton<IInteractionService, ConsoleInteractionService>();
        services.AddSingleton<BannerService>();

        services.ConfigureHttpClientDefaults(http =>
        {
            http.ConfigureHttpClient(client => client.MaxResponseContentBufferSize = BlobClient.MaxResponseBytes);
            http.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        });

        services.AddHttpClient(BlobClient.HttpClientName);
        services.AddHttpClient(WellKnownProvider.HttpClientName);

        services.AddSingleton<IGitClient, GitClient>();
        services.AddSingleton<IGitHubTokenProvider, GitHubTokenProvider>();
        services.AddSingleton<IBlobClient, BlobClient>();

        services.AddSingleton<ISystemEnvironment, SystemEnvironment>();
        services.AddSingleton<IFileStore, SystemFileStore>();
        services.AddSingleton<AgentRegistry>();
        services.AddSingleton<AgentEnvironment>();
        services.AddSingleton<XdgPaths>();
        services.AddSingleton<ISkillInstaller, SkillInstaller>();

        services.AddSingleton<PluginManifest>();
        services.AddSingleton<PluginGrouping>();
        services.AddSingleton<ISkillDiscovery, SkillDiscovery>();

        services.AddSingleton<ISourceParser, SourceParser>();

        services.AddSingleton<IProvider, GitHubProvider>();
        services.AddSingleton<IProvider, GitLabProvider>();
        services.AddSingleton<IProvider, GitProvider>();
        services.AddSingleton<IProvider, LocalProvider>();
        services.AddSingleton<IProvider, WellKnownProvider>();
        services.AddSingleton<ProviderRegistry>();

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IProjectLockFile, ProjectLockFile>();
        services.AddSingleton<IGlobalLockFile, GlobalLockFile>();

        services.AddSingleton<IAddCommandPrompter, AddCommandPrompter>();
        services.AddSingleton<IRemoveCommandPrompter, RemoveCommandPrompter>();

        services.AddTransient<AddCommandExecutor>();
        services.AddTransient<AddCommand>();
        services.AddTransient<RemoveCommand>();
        services.AddTransient<ListCommand>();
        services.AddTransient<InitCommand>();
        services.AddTransient<UpdateCommand>();
        services.AddTransient<SkillsRootCommand>();

        return services;
    }
}
