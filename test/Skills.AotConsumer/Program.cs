using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Skills.Commands;
using Skills.Extensions;

// The host pattern from docs/embedding.md: a host builds its own service
// collection, calls AddSkillsServices, and composes SkillsCommand as a subcommand of its own
// root. This is the consumer-side proof that referencing Skills.csproj and publishing AOT stays
// warning-free; it is not the shipping CLI (that is src/Skills.Cli).
var services = new ServiceCollection();
services.AddSkillsServices(toolCommandName: "aotconsumer skills");
await using var provider = services.BuildServiceProvider();

var rootCommand = new RootCommand("Sample host that embeds the skills commands");
rootCommand.Subcommands.Add(new SkillsCommand(provider));

return await rootCommand.Parse(args).InvokeAsync();
