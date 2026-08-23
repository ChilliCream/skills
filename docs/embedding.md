# Embedding contract: skills commands in a host CLI

Status: design contract for the skillz-rn6 epic (task skillz-rn6.1). No production
code changes in this task. The follow-up tasks (project split, public surface,
embeddable composition, sample host) implement exactly what is written here.

Decisions 1, 2 and 3 are ruled here.

This document lives at docs/embedding.md rather than as a CLAUDE.md section
because it is a host-facing API contract, not an internal coding convention.

## Public surface

After the epic, the library exposes exactly two public types. Everything else
stays `internal`.

```csharp
namespace Skills.Commands;

/// The embeddable command group. A host adds it as a subcommand of its own
/// root and gets `<host> skills add|remove|list|init|update`.
public sealed class SkillsCommand : Command
{
    public SkillsCommand(IServiceProvider serviceProvider);
}
```

```csharp
namespace Skills.Extensions;

public static class ServiceCollectionExtensions
{
    /// Registers every service the skills commands resolve at execution time.
    /// toolCommandName is the full prefix a user types to reach the skills
    /// verbs: "skills" (or "skillz") standalone, "nitro skills" when embedded.
    public static IServiceCollection AddSkillsServices(
        this IServiceCollection services,
        string toolCommandName);
}
```

`AddSkillsServices` already exists with this exact signature (landed in
skillz-zy4.7); the epic only changes its visibility from `internal` to
`public`. The `toolCommandName` parameter feeds the `CliExecutionContext`
singleton, which is what renders example lines in `--help` output and
command hints; that is why the host must pass the real prefix including its
own executable name. Note that Nitro's own `AddNitroServices`-style overload
is parameterless; ours is not, and the host snippet below reflects that.

Registrations use plain `Add*`, not `TryAdd*`. The host shares one container
with the skills commands; if it needs to override a registration (for
example console wiring), it registers its replacement after calling
`AddSkillsServices`.

## Host snippet (what Nitro would write)

```csharp
using Skills.Commands;
using Skills.Extensions;

var services = new ServiceCollection();
// ... host's own registrations ...
services.AddSkillsServices(toolCommandName: "nitro skills");
await using var provider = services.BuildServiceProvider();

var rootCommand = new NitroRootCommand();
rootCommand.Subcommands.Add(new SkillsCommand(provider));

return await rootCommand.Parse(args).InvokeAsync();
```

Constraint: the provider must exist when `SkillsCommand` is constructed. A
host that only materializes its container during invocation composes the
`SkillsCommand` at that point. A lazy `SkillsCommand(Func<IServiceProvider>)`
overload would be a compatible additive change if a real host needs it; it is
deliberately not part of v1.

The snippet above is not prose only: `samples/EmbeddedHost` is a runnable
project that composes it, with a command of its own next to `SkillsCommand`
so the wiring is end to end rather than a skills-only root. It also
publishes AOT in CI, so it is the consumer-side proof that referencing the
library and publishing AOT stays warning-free; one project serves as both
the sample and the AOT proof. See the README for how to run it.

## Nitro integration note

This is the specific shape a `graphql-platform` maintainer would add, not a
general pattern; see "Host snippet" above for the general one.

`NitroRootCommand` is `internal sealed` with a parameterless constructor;
Nitro builds its provider in `Program.cs` and passes it to `ExecuteAsync` at
invocation, not to the root command's constructor. Embedding therefore needs
one of two explicit edits on Nitro's side:

- (a) add `SkillsCommand` in `Program.cs`, after `BuildServiceProvider` and
  before `ExecuteAsync`, leaving `NitroRootCommand` itself untouched:

  ```csharp
  var provider = services.BuildServiceProvider();
  var rootCommand = new NitroRootCommand();
  rootCommand.Subcommands.Add(new SkillsCommand(provider));

  return await rootCommand.ExecuteAsync(args, provider, null, cts.Token);
  ```

- (b) give `NitroRootCommand` an `IServiceProvider` constructor parameter and
  compose `SkillsCommand` inside it instead; this also requires updating
  every `new NitroRootCommand()` call site, including the one in
  `GlobalOptionsTests.cs`.

Option (a) is shown above because it matches the "Host snippet" section's
`new NitroRootCommand()` call and needs no changes to Nitro's existing
constructor call sites.

Service registration: call `services.AddSkillsServices(toolCommandName: "nitro skills")`
on the same `IServiceCollection` that `AddNitroServices()` populates, either
before or after that call, then build the provider once. `AddNitroServices`
registers throughout with `TryAddSingleton`, and both sides register
`TimeProvider.System`: Nitro via `TryAddSingleton`, Skills via `AddSingleton`
(`src/Skills/Extensions/ServiceCollectionExtensions.cs:72`). Order stays
harmless not because the registrations avoid overlap, but because whichever
side registers first, the resolved `TimeProvider` is the same
`TimeProvider.System` instance either way.

Friction point to flag for whoever does this: Nitro resolves its own
services through its own `internal` `CommandExecutionContext` `AsyncLocal`,
which is a different type in a different assembly from anything in this
package (this package deleted its equivalent `AsyncLocal` per Decision 2
below). Skills commands never read Nitro's ambient context and there is
nothing to populate on Nitro's side; the only thing the host must do is pass
the `IServiceProvider` it built into `new SkillsCommand(services)` at
composition time, as shown above. It is easy to assume services flow
through the ambient context the way the rest of a Nitro command tree does;
here they explicitly do not.

## Decision 1: what a host adds to its root command (RULED)

Ruling: adopt the proposed shape.

- `public sealed class SkillsCommand : Command`, named `"skills"`, composes
  the five subcommands (`AddCommand`, `RemoveCommand`, `ListCommand`,
  `InitCommand`, `UpdateCommand`).
- `internal sealed class SkillsRootCommand : RootCommand` keeps existing for
  the standalone CLI and composes the same five subcommands directly at the
  root, so `skills add ...` is unchanged.
- Both build from one internal composition helper so they never drift:

```csharp
namespace Skills.Commands;

internal static class SkillsSubcommands
{
    public static void AddTo(Command command)
    {
        command.Subcommands.Add(new AddCommand());
        command.Subcommands.Add(new RemoveCommand());
        command.Subcommands.Add(new ListCommand());
        command.Subcommands.Add(new InitCommand());
        command.Subcommands.Add(new UpdateCommand());
    }
}
```

Rationale: a `RootCommand` cannot be added as a subcommand of another
command, so the reusable unit cannot be `SkillsRootCommand`. The shape
mirrors the reference (`NitroRootCommand` composes `new ApiCommand()`,
`new AgentCommand()`, ...). `CommandExamples.Install` takes a `RootCommand`
(it wraps the root's `HelpOption`) and stays a `SkillsRootCommand`-only
call; see "Behavior differences when embedded" below.

## Decision 2: how the host supplies services (RULED)

Ruling: option (b), as the single mechanism for both paths. The
`CommandExecutionContext` `AsyncLocal` is deleted; there is no ambient
state and no setup call a host can forget.

- `SkillsCommand(IServiceProvider)` and `SkillsRootCommand(IServiceProvider)`
  each capture the provider, wrapped in the existing `CommandServices`
  adapter, and expose it through a new internal interface:

```csharp
namespace Skills;

internal interface ICommandServicesSource
{
    ICommandServices CommandServices { get; }
}
```

- `CommandExtensions.SetActionWithExceptionHandling` stops reading
  `CommandExecutionContext.s_services` and instead resolves services by
  walking from `parseResult.CommandResult.Command` up `Symbol.Parents`
  (public in System.CommandLine 2.0.5, the pinned version) to the nearest
  command implementing `ICommandServicesSource`. In both trees that is one
  hop: leaf to `SkillsCommand` or `SkillsRootCommand`.
- If no source is found the wrapper throws `InvalidOperationException`
  naming the contract, for example: "AddCommand must be composed under
  SkillsCommand or SkillsRootCommand constructed with the IServiceProvider
  built from AddSkillsServices." In practice this is unreachable from
  outside the package because the leaf commands are internal, but it is the
  required failure mode, not a null dereference.
- `ExamplesHelpAction` resolves `CliExecutionContext.CommandName` through
  the same parent walk (null-tolerant variant) and keeps its `"skills"`
  fallback.
- `RootCommandExtensions.ExecuteAsync` stops setting the `AsyncLocal`;
  `CommandExecutionContext` and `s_services` are deleted.
- The standalone entry point (`Program.RunAsync`) already builds the
  provider before constructing the root command; it changes to
  `new SkillsRootCommand(provider)`.

Rationale: (a) ambient context requires the host to remember a setup call,
and the reference host runs its own pipeline with its own execution context
type in a different assembly, so nothing forces the call to happen. (c) is
two mechanisms. (b) is host-agnostic, compiler-enforced at the only public
construction point, AOT-friendly, and removes static mutable state that
leaks across tests. The parameterless-constructor convention from
skillz-zy4 continues to apply to the five `{Verb}Command` leaves and their
static `ExecuteAsync(ICommandServices, ParseResult, CancellationToken)`
handlers; the two composition roots are the seam where DI enters and are
the deliberate exception. The CLAUDE.md conventions section must be updated
with this exception in the task that implements it (skillz-rn6.3), and
tests that seed the `AsyncLocal` directly move to constructing
`SkillsRootCommand`/`SkillsCommand` with a provider.

## Behavior differences when embedded

The standalone path (`RootCommandExtensions.ExecuteAsync`) carries curated
behavior that an embedding host's pipeline will not run. The contract:

- Lost when embedded, by design: the zero-args banner, the curated
  top-level `--help`, the logo before `add`/`init`, and bare `--` stripping.
  The host owns its root-level experience; `nitro skills --help` renders
  System.CommandLine's standard help.
- Example lines under `--help` are installed by `CommandExamples.Install`
  on the standalone root's `HelpOption`. A host's `HelpOption` belongs to
  the host root, so embedded help does not render the example lines. This
  is accepted; it is a presentation nicety, not part of the contract.
- Must work identically when embedded: the `--json`/`--format json` switch
  to machine-readable output. Its central resolution currently lives in
  `RootCommandExtensions.ExecuteAsync` and would be skipped by a host
  pipeline. Ruling: move that resolution into the
  `SetActionWithExceptionHandling` wrapper (which runs on every invocation
  in both paths and already has the `ParseResult`), and delete it from
  `ExecuteAsync`. Behavior on the parse-error path is unchanged (neither
  runs the wrapper today).
- The exception-to-exit-code ladder and cancellation mapping live in the
  wrapper and therefore already work identically when embedded.

## Decision 3: package identity (RULED)

Ruling: the package id stays `Skills.Library`, the value already on
`Skills.csproj` before this decision was ruled. The proposed
`ChilliCream.Skills.CommandLine` (mirroring `ChilliCream.Nitro.CommandLine`)
was not adopted. `IsPackable` is `true` and the library packs and publishes
under exactly `Skills.Library` (skillz-rn6.6).

`src/Skills.Tool` publishes the tool package ids `skills` and `skillz`
(`ToolCommandName` `skills`); `Skills.Library` collides with neither.

The root namespace stays `Skills.*`; a `ChilliCream.Skills.CommandLine` id
would have raised the question of a `ChilliCream.*` namespace to match, but
since that id was not adopted the namespace is unaffected and remains
`Skills.*`.

## Project layout after the split (for skillz-rn6.2)

- `src/Skills` becomes the class library, packed and published as
  `Skills.Library` (skillz-rn6.6). It keeps everything it has today except
  the process entry point: the five commands,
  `SkillsCommand` (new), `SkillsRootCommand`, `RootCommandExtensions`,
  `BannerService`, services, options. Public surface as listed above; the
  standalone-only pieces stay internal.
- new `src/Skills.Cli` is the slim executable: `Program.Main`/`RunAsync`
  (including the CancelKeyPress/ProcessExit wiring and the top-level catch
  ladder) move there. It carries the AOT publish settings that only make
  sense for an executable.
- `src/Skills.Tool` keeps shipping the `skills`/`skillz` tool packages and
  calls `RunAsync` where it now lives, referencing `Skills.Cli`.
- `InternalsVisibleTo` from the library: `Skills.Tests`, and the assembly
  name `skills` (`Skills.Cli`'s `AssemblyName`, not the project name). There
  is no `Skills.Tool` grant; it only calls the public
  `Skills.Cli.Program.RunAsync`.

## Non-goals

- No plugin or extensibility model. A host embeds the five commands as they
  are; it does not add its own skills subcommands.
- No lazy-provider constructor overload in v1 (see host snippet section).
- This document changes no production code; the gate baseline (0-warning
  build, full test suite green) is unchanged by it.
