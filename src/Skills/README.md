# Skills.Library

Embeddable [System.CommandLine](https://github.com/dotnet/command-line-api)
commands for managing AI agent skills. A host CLI adds `SkillsCommand` as a
subcommand of its own root and gets `<host> skills add|remove|list|init|update`
without shelling out to a separate process.

Skills are markdown files (`SKILL.md`) with YAML frontmatter that teach AI
coding agents how to do something. These commands install them from GitHub,
GitLab, generic Git repos, or local paths, into whichever agents are present
on the machine.

## Install

```bash
dotnet add package Skills.Library
```

## Usage

```csharp
using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Skills.Commands;
using Skills.Extensions;

var services = new ServiceCollection();
// ... host's own registrations ...
services.AddSkillsServices(toolCommandName: "myhost skills");
await using var provider = services.BuildServiceProvider();

var rootCommand = new MyHostRootCommand();
rootCommand.Subcommands.Add(new SkillsCommand(provider));

return await rootCommand.Parse(args).InvokeAsync();
```

The public surface is exactly `SkillsCommand` and `AddSkillsServices`; the
full embedding contract, including what differs from the standalone `skills`
CLI, is documented in
[`docs/embedding.md`](https://github.com/ChilliCream/skills/blob/main/docs/embedding.md).
