using Skills.Interaction;

namespace Skills.Extensions;

/// <summary>
/// Parses and invokes the Skills root command, wiring <see cref="CommandExecutionContext"/> so
/// command actions can resolve services, and preserving the CLI's curated first-run experience
/// (the zero-args banner, the top-level curated help, and the logo shown before <c>add</c>/<c>init</c>)
/// ahead of System.CommandLine's own parsing and invocation.
/// </summary>
internal static class RootCommandExtensions
{
    public static async Task<int> ExecuteAsync(
        this RootCommand rootCommand,
        IReadOnlyList<string> args,
        IServiceProvider services,
        InvocationConfiguration? invocationConfiguration,
        CancellationToken cancellationToken)
    {
        CommandExecutionContext.s_services.Value = new CommandServices(services);

        var strippedArgs = Program.StripBareTerminators(args);

        if (strippedArgs.Length == 0)
        {
            var banner = services.GetRequiredService<BannerService>();
            banner.ShowBanner();
            return ExitCodeConstants.Success;
        }

        // Curated root help: short-circuit when user asks for top-level help only
        if (strippedArgs.Length == 1 && strippedArgs[0] is "--help" or "-h" or "-?")
        {
            var banner = services.GetRequiredService<BannerService>();
            banner.ShowLogo();
            banner.ShowCuratedHelp();
            return ExitCodeConstants.Success;
        }

        var parseResult = rootCommand.Parse(strippedArgs);

        if (parseResult.Errors.Count > 0)
        {
            return await parseResult.InvokeAsync(invocationConfiguration, cancellationToken);
        }

        var commandName = parseResult.CommandResult.Command.Name;
        if (commandName is "add" or "init")
        {
            var banner = services.GetRequiredService<BannerService>();
            banner.ShowLogo();
        }

        return await parseResult.InvokeAsync(invocationConfiguration, cancellationToken);
    }
}
