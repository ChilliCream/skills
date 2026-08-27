using Skills.Interaction;

namespace Skills.Extensions;

/// <summary>
/// Provides helpers for running the Skills root command.
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

        var strippedArgs = StripBareTerminators(args);

        if (strippedArgs.Length == 0)
        {
            var banner = services.GetRequiredService<BannerService>();
            banner.ShowBanner();
            return ExitCodeConstants.Success;
        }

        // Show curated help for the root command.
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

    // The CLI has no pass-through commands, so bare terminators can be ignored.
    internal static string[] StripBareTerminators(IReadOnlyList<string> args) => args.Where(a => a != "--").ToArray();
}
