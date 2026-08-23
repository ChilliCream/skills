<p align="center">
  <img src="assets/logo.svg" alt="Skills" width="120" height="120" />
</p>

# Skills

A CLI for managing AI agent skills - markdown `SKILL.md` files with YAML
frontmatter that teach AI coding agents how to do specific tasks.

```bash
# One-shot run, no install (.NET 10+ SDK)
dnx skills add chillicream/agent-skills

# Or persistent install
dotnet tool install -g skills
```

The NuGet package exposes the `skills` command.

End-user documentation - including the full `dnx` reference - lives in
[`src/Skills.Tool/README.md`](src/Skills.Tool/README.md) and is what ships on
NuGet.

## What it does

* Installs skills from GitHub, GitLab, generic Git repos, or local paths
* Targets 55+ AI coding agents (Claude Code, Cursor, Copilot, Codex, Continue,
  Gemini CLI, …) - auto-detected on your machine
* Symlinks by default from one canonical location so all agents stay in sync;
  `--copy` for agents that don't follow symlinks
* Two scopes: project (`skills-lock.json` in cwd) and global (XDG state dir)
* Scaffolds new skills with `skills init`

## Repository layout

```
src/
  Skills/           Main CLI assembly (AOT-publishable binary)
  Skills.Tool/      `dotnet tool` wrapper for the `skills` NuGet package
test/
  Skills.Tests/     Unit tests
  Skills.SmokeTests/ End-to-end smoke tests
samples/
  EmbeddedHost/     Runnable sample host embedding the skills commands
Skills.sln          Solution
global.json         .NET SDK pin
```

`Skills.Tool` is a thin wrapper that calls into `Skills.Program.Main`. It exists
so `dotnet tool install -g skills` exposes the `skills` command while `Skills`
itself can also be AOT-published as a standalone binary for the supported runtime
identifiers (`linux-x64`, `linux-arm64`, `linux-musl-x64`, `osx-x64`, `osx-arm64`,
`win-x64`, `win-arm64`).

## Embedding in another CLI

`Skills.Library` on NuGet is the same commands as a package, so another CLI
can add them as a subcommand group instead of shelling out to a separate
`skills` process.

Install:

```bash
dotnet add package Skills.Library
```

Register the services and compose the command:

```csharp
using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Skills.Commands;
using Skills.Extensions;

var services = new ServiceCollection();
// ... host's own registrations ...
services.AddSkillsServices(toolCommandName: "myapp skills");
await using var provider = services.BuildServiceProvider();

var rootCommand = new MyAppRootCommand();
rootCommand.Subcommands.Add(new SkillsCommand(provider));

return await rootCommand.Parse(args).InvokeAsync();
```

`toolCommandName` must be the full prefix a user types to reach the skills
verbs (`"myapp skills"`, not `"skills"`) - it feeds the `--help` example
lines and the "next steps" hints the commands print.

The library sets `IsAotCompatible`, so a host publishing with
`PublishAot=true` stays warning-free after adding this dependency.

`samples/EmbeddedHost` is that snippet as a runnable project, with one
command of its own (`version`) alongside the skills commands to show the
wiring end to end:

```bash
dotnet run --project samples/EmbeddedHost -- --help
dotnet run --project samples/EmbeddedHost -- skills list
dotnet run --project samples/EmbeddedHost -- skills init demo
```

`--help` lists `skills` among its subcommands:

```
Description:
  Sample host that embeds the skills commands

Usage:
  embeddedhost [command] [options]

Options:
  -?, -h, --help  Show help and usage information
  --version       Show version information

Commands:
  version  Show the embedded host's own version
  skills   Manage AI agent skills
```

The full contract - what a host gains and loses versus the standalone CLI,
and the design decisions behind it, including a note for Nitro specifically
- is in [`docs/embedding.md`](docs/embedding.md).

## Build

```bash
dotnet build
```

Targets `net8.0` and `net9.0`. Requires the .NET SDK pinned in `global.json`.

## Test

```bash
dotnet test
```

## Run locally

```bash
dotnet run --project src/Skills.Cli -- add anthropics/skills
```

## Publish AOT

```bash
dotnet publish src/Skills.Cli -c Release -r linux-x64
```

Produces a single self-contained `skills` binary at
`src/Skills.Cli/bin/Release/<tfm>/linux-x64/publish/skills`.

## Pack the tool

```bash
dotnet pack src/Skills.Tool -c Release -o ./artifacts \
  -p:SkillsToolPackageId="skills"
```

Produces `artifacts/skills.<version>.nupkg`; the package installs the `skills`
command.

## Contributing

Issues and PRs welcome. Before sending a PR:

```bash
dotnet build
dotnet test
```

## License

[MIT](LICENSE)
