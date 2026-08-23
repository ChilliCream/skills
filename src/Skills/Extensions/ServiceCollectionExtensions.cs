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
/// Registers the services the skills commands need with a host's dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers every service the skills commands resolve at execution time.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="toolCommandName">
    /// The full prefix a user types to reach the skills verbs: <c>"skills"</c> (or <c>"skillz"</c>)
    /// standalone, <c>"nitro skills"</c> when embedded. This feeds the execution context that
    /// renders example lines in <c>--help</c> output and command hints, so callers must pass the
    /// real prefix including their own executable name.
    /// </param>
    /// <returns>The same <paramref name="services"/> instance, so calls can be chained.</returns>
    public static IServiceCollection AddSkillsServices(this IServiceCollection services, string toolCommandName)
    {
        services.AddSingleton(AnsiConsole.Console);
        services.AddSingleton<ConsoleEnvironment>();
        services.AddSingleton(new CliExecutionContext { CommandName = toolCommandName });
        services.AddSingleton<IInteractionService, ConsoleInteractionService>();
        services.AddSingleton<BannerService>();

        services.AddHttpClient(BlobClient.HttpClientName)
            .ConfigureHttpClient(client => client.MaxResponseContentBufferSize = BlobClient.MaxResponseBytes)
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        services.AddHttpClient(WellKnownProvider.HttpClientName)
            .ConfigureHttpClient(client => client.MaxResponseContentBufferSize = BlobClient.MaxResponseBytes)
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });

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

        return services;
    }
}
