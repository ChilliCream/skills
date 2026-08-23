using Skills.Interaction;

namespace Skills.Extensions;

/// <summary>
/// Parses and invokes the Skills root command, preserving the CLI's curated first-run experience
/// (the zero-args banner, the top-level curated help, and the logo shown before <c>add</c>/<c>init</c>)
/// ahead of System.CommandLine's own parsing and invocation. The root command itself already
/// carries the services (see <c>SkillsRootCommand</c>'s constructor), so <c>services</c> here is
/// only used directly for the banner and curated-help calls that happen before a command action
/// ever runs.
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
        var strippedArgs = StripBareTerminators(args);

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

    // Strips bare `--` tokens. The CLI has no pass-through commands, so the argument terminator
    // is meaningless here and would only confuse System.CommandLine's parsing.
    internal static string[] StripBareTerminators(IReadOnlyList<string> args) => args.Where(a => a != "--").ToArray();
}
