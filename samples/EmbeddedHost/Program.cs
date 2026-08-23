using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Skills.Commands;
using Skills.Extensions;

// The host pattern from docs/embedding.md: a host builds its own service collection, calls
// AddSkillsServices, and composes SkillsCommand as a subcommand of its own root next to its own
// commands. This project is the runnable version of that pattern for README.md and
// docs/embedding.md, and it doubles as the consumer-side proof that referencing Skills.csproj and
// publishing AOT stays warning-free; it is not the shipping CLI (that is src/Skills.Cli).
var services = new ServiceCollection();
// ... a real host's own registrations go here ...
services.AddSkillsServices(toolCommandName: "embeddedhost skills");
await using var provider = services.BuildServiceProvider();

var rootCommand = new RootCommand("Sample host that embeds the skills commands");
rootCommand.Subcommands.Add(VersionCommand());
rootCommand.Subcommands.Add(new SkillsCommand(provider));

return await rootCommand.Parse(args).InvokeAsync();

// A command that belongs to the host, not to the skills package, showing that the two compose
// side by side under one root.
static Command VersionCommand()
{
    var command = new Command("version", "Show the embedded host's own version");
    command.SetAction(_ => Console.WriteLine("embeddedhost 1.0.0"));
    return command;
}
