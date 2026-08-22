using Microsoft.Extensions.Hosting;
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

namespace Skills;

internal static class Program
{
    public static Task<int> Main(string[] args) => RunAsync(args, toolCommandName: null);

    internal static async Task<int> RunAsync(string[] args, string? toolCommandName)
    {
        // Strip bare `--` tokens — the CLI has no pass-through commands, so the argument
        // terminator is meaningless here and would only confuse System.CommandLine's parsing.
        args = StripBareTerminators(args);
        toolCommandName = string.IsNullOrWhiteSpace(toolCommandName) ? "skills" : toolCommandName.Trim();

        using var cts = new CancellationTokenSource();

        ConsoleCancelEventHandler cancelKeyHandler = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cts.Cancel();
        };
        Console.CancelKeyPress += cancelKeyHandler;

        EventHandler unloadHandler = (_, _) => { if (!cts.IsCancellationRequested)
        {
            cts.Cancel();
        } };
        AppDomain.CurrentDomain.ProcessExit += unloadHandler;

        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings { Args = args });

        builder.Services.AddSingleton(AnsiConsole.Console);
        builder.Services.AddSingleton<ConsoleEnvironment>();
        builder.Services.AddSingleton(new CliExecutionContext { CommandName = toolCommandName });
        builder.Services.AddSingleton<IInteractionService, ConsoleInteractionService>();
        builder.Services.AddSingleton<BannerService>();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.ConfigureHttpClient(client => client.MaxResponseContentBufferSize = BlobClient.MaxResponseBytes);
            http.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        });

        builder.Services.AddHttpClient(BlobClient.HttpClientName);
        builder.Services.AddHttpClient(WellKnownProvider.HttpClientName);

        builder.Services.AddSingleton<IGitClient, GitClient>();
        builder.Services.AddSingleton<IGitHubTokenProvider, GitHubTokenProvider>();
        builder.Services.AddSingleton<IBlobClient, BlobClient>();

        builder.Services.AddSingleton<ISystemEnvironment, SystemEnvironment>();
        builder.Services.AddSingleton<IFileStore, SystemFileStore>();
        builder.Services.AddSingleton<AgentRegistry>();
        builder.Services.AddSingleton<AgentEnvironment>();
        builder.Services.AddSingleton<XdgPaths>();
        builder.Services.AddSingleton<ISkillInstaller, SkillInstaller>();

        builder.Services.AddSingleton<PluginManifest>();
        builder.Services.AddSingleton<PluginGrouping>();
        builder.Services.AddSingleton<ISkillDiscovery, SkillDiscovery>();

        builder.Services.AddSingleton<ISourceParser, SourceParser>();

        builder.Services.AddSingleton<IProvider, GitHubProvider>();
        builder.Services.AddSingleton<IProvider, GitLabProvider>();
        builder.Services.AddSingleton<IProvider, GitProvider>();
        builder.Services.AddSingleton<IProvider, LocalProvider>();
        builder.Services.AddSingleton<IProvider, WellKnownProvider>();
        builder.Services.AddSingleton<ProviderRegistry>();

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<IProjectLockFile, ProjectLockFile>();
        builder.Services.AddSingleton<IGlobalLockFile, GlobalLockFile>();

        builder.Services.AddSingleton<IAddCommandPrompter, AddCommandPrompter>();
        builder.Services.AddSingleton<IRemoveCommandPrompter, RemoveCommandPrompter>();

        builder.Services.AddTransient<AddCommandExecutor>();
        builder.Services.AddTransient<AddCommand>();
        builder.Services.AddTransient<RemoveCommand>();
        builder.Services.AddTransient<ListCommand>();
        builder.Services.AddTransient<InitCommand>();
        builder.Services.AddTransient<UpdateCommand>();
        builder.Services.AddTransient<SkillsRootCommand>();

        using var host = builder.Build();

        try
        {
            await host.StartAsync(cts.Token);

            var rootCommand = host.Services.GetRequiredService<SkillsRootCommand>();

            if (args.Length == 0)
            {
                var banner = host.Services.GetRequiredService<BannerService>();
                banner.ShowBanner();
                return ExitCodeConstants.Success;
            }

            // Curated root help: short-circuit when user asks for top-level help only
            if (args.Length == 1 && args[0] is "--help" or "-h" or "-?")
            {
                var banner = host.Services.GetRequiredService<BannerService>();
                banner.ShowLogo();
                banner.ShowCuratedHelp();
                return ExitCodeConstants.Success;
            }

            var parseResult = rootCommand.Parse(args);

            var commandName = parseResult.CommandResult.Command.Name;
            if (commandName is "add" or "init")
            {
                var banner = host.Services.GetRequiredService<BannerService>();
                banner.ShowLogo();
            }

            return await parseResult.InvokeAsync(cancellationToken: cts.Token);
        }
        catch (OperationCanceledException)
        {
            return ExitCodeConstants.Cancelled;
        }
        catch (CliException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return ex.ExitCode;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return ExitCodeConstants.Failure;
        }
        finally
        {
            Console.CancelKeyPress -= cancelKeyHandler;
            AppDomain.CurrentDomain.ProcessExit -= unloadHandler;
            await host.StopAsync();
        }
    }

    internal static string[] StripBareTerminators(string[] args) => args.Where(a => a != "--").ToArray();
}
