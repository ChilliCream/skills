using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
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
using Skills.Tests.TestServices;
using Skills.Utils;

namespace Skills.Tests.Utils;

internal static class CliTestHelper
{
    public static IServiceProvider CreateServiceProvider(
        string? workspace = null,
        Action<IServiceCollection>? configure = null,
        bool useRealFileStore = false)
    {
        var services = new ServiceCollection();

        services.AddSingleton<TestConsoleEnvironment>();
        services.AddSingleton<ConsoleEnvironment>(sp => sp.GetRequiredService<TestConsoleEnvironment>());
        services.AddSingleton<CliExecutionContext>();

        services.AddSingleton<TestInteractionService>();
        services.AddSingleton<IInteractionService>(sp => sp.GetRequiredService<TestInteractionService>());

        services.AddSingleton<TestGitClient>();
        services.AddSingleton<IGitClient>(sp => sp.GetRequiredService<TestGitClient>());

        services.AddSingleton<TestBlobClient>();
        services.AddSingleton<IBlobClient>(sp => sp.GetRequiredService<TestBlobClient>());

        services.AddSingleton<TestGitHubTokenProvider>();
        services.AddSingleton<IGitHubTokenProvider>(sp => sp.GetRequiredService<TestGitHubTokenProvider>());

        services.AddSingleton<ISystemEnvironment>(new FakeSystemEnvironment
        {
            HomeDirectory = "/home/test",
            CurrentDirectory = workspace ?? "/workspace"
        });

        services.AddSingleton<FakeFileStore>();
        if (useRealFileStore)
        {
            // Integration-style command tests create real files under a temp workspace and assert
            // against the real filesystem, so they run on the production SystemFileStore.
            services.AddSingleton<IFileStore, SystemFileStore>();
        }
        else
        {
            services.AddSingleton<IFileStore>(sp => sp.GetRequiredService<FakeFileStore>());
        }

        services.AddSingleton<AgentRegistry>();
        services.AddSingleton<AgentEnvironment>();

        services.AddSingleton<TestInstaller>();
        services.AddSingleton<ISkillInstaller>(sp => sp.GetRequiredService<TestInstaller>());

        services.AddSingleton<TestSkillDiscovery>();
        services.AddSingleton<ISkillDiscovery>(sp => sp.GetRequiredService<TestSkillDiscovery>());

        services.AddSingleton<TestSourceParser>();
        services.AddSingleton<ISourceParser>(sp => sp.GetRequiredService<TestSourceParser>());

        services.AddSingleton<TestProjectLockFile>();
        services.AddSingleton<IProjectLockFile>(sp => sp.GetRequiredService<TestProjectLockFile>());

        services.AddSingleton<TestGlobalLockFile>();
        services.AddSingleton<IGlobalLockFile>(sp => sp.GetRequiredService<TestGlobalLockFile>());

        services.AddSingleton<TestAddCommandPrompter>();
        services.AddSingleton<IAddCommandPrompter>(sp => sp.GetRequiredService<TestAddCommandPrompter>());
        services.AddSingleton<TestRemoveCommandPrompter>();
        services.AddSingleton<IRemoveCommandPrompter>(sp => sp.GetRequiredService<TestRemoveCommandPrompter>());

        services.AddSingleton<IProvider>(sp => new GitHubProvider(
            sp.GetRequiredService<IGitClient>(),
            sp.GetRequiredService<ISkillDiscovery>()));
        services.AddSingleton<IProvider>(sp => new GitLabProvider(
            sp.GetRequiredService<IGitClient>(),
            sp.GetRequiredService<ISkillDiscovery>()));
        services.AddSingleton<IProvider>(sp => new GitProvider(
            sp.GetRequiredService<IGitClient>(),
            sp.GetRequiredService<ISkillDiscovery>()));
        services.AddSingleton<IProvider>(sp => new LocalProvider(
            sp.GetRequiredService<ISkillDiscovery>(),
            sp.GetRequiredService<IFileStore>()));
        services.AddSingleton<ProviderRegistry>();

        services.AddTransient<AddCommandExecutor>();

        // Commands resolved directly (rather than through SkillsRootCommand/SkillsCommand) still
        // need an ICommandServicesSource ancestor for CommandServicesResolver to find, the same
        // way a real composition root provides one. Each factory parents the freshly built
        // command under a throwaway root wired to this same provider before handing it back, so
        // `services.GetRequiredService<AddCommand>()` followed by `cmd.Parse(args)` keeps working
        // unchanged at every call site.
        services.AddTransient<AddCommand>(sp => AttachCommandServices(sp, new AddCommand()));
        services.AddTransient<RemoveCommand>(sp => AttachCommandServices(sp, new RemoveCommand()));
        services.AddTransient<ListCommand>(sp => AttachCommandServices(sp, new ListCommand()));
        services.AddTransient<InitCommand>(sp => AttachCommandServices(sp, new InitCommand()));
        services.AddTransient<UpdateCommand>(sp => AttachCommandServices(sp, new UpdateCommand()));
        services.AddTransient<SkillsRootCommand>();

        if (workspace is not null)
        {
            services.AddSingleton(new TestWorkspace(workspace));
        }

        configure?.Invoke(services);

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Parents <paramref name="command"/> under a throwaway root implementing
    /// <see cref="ICommandServicesSource"/> so <see cref="CommandServicesResolver"/> resolves the
    /// fakes <paramref name="services"/> registers when the command is parsed and invoked
    /// directly, the same way a real host's <c>SkillsCommand</c> composition does. Returns
    /// <paramref name="command"/> so it composes into a DI factory registration.
    /// </summary>
    public static TCommand AttachCommandServices<TCommand>(IServiceProvider services, TCommand command)
        where TCommand : Command
    {
        var root = new TestServicesRoot(services);
        root.Subcommands.Add(command);
        return command;
    }
}

internal sealed record TestWorkspace(string Path);

/// <summary>
/// A minimal <see cref="ICommandServicesSource"/> root used to parent a command under test so it
/// resolves services the same way <c>SkillsRootCommand</c>/<c>SkillsCommand</c> do in production.
/// </summary>
internal sealed class TestServicesRoot(IServiceProvider services) : RootCommand("test-root"), ICommandServicesSource
{
    ICommandServices ICommandServicesSource.CommandServices { get; } = new CommandServices(services);
}
