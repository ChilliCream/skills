# CLI layer conventions

`src/Skills/Commands/` follows the System.CommandLine conventions summarized
below.

- **Command class**: `internal sealed class {Verb}Command : Command`, a
  parameterless constructor. Constructor order: base(name, description),
  `Arguments.Add`, `Options.Add`, `Validators.Add` (rare, for cross-option
  checks System.CommandLine can't express on a single option), `AddExamples`,
  and `SetActionWithExceptionHandling` last. The handler is
  `private static async Task<int> ExecuteAsync(ICommandServices services, ParseResult parseResult, CancellationToken cancellationToken)`.
- **Options and arguments**: one class per option/argument, each exposing
  `public const string OptionName` (or `ArgumentName`). Options shared
  verbatim across commands live under `src/Skills/Options/`; options that are
  merely similarly-named but command-specific (different help text, different
  default) get their own class under `src/Skills/Commands/<Command>/Options/`
  (see the four separate `GlobalOption` classes for `add`, `list`, `remove`,
  `update`). Always register and read through `Opt<T>.Instance`, never `new`:
  System.CommandLine matches options/arguments by reference, so the registered
  instance and the one read back in `ExecuteAsync` must be the same object.
- **Errors**: throw `ExitException` for CLI-layer aborts; an empty-message
  `ExitException` means the command already reported detail through
  `IInteractionService`. Throw `CliException` for domain errors that carry an
  `ExitCode` plus optional `Title`/`Hint` rendered as an error panel. Return
  codes from `ExitCodeConstants` (`Success`, `Failure`, `Cancelled`). Never
  call `Environment.Exit`; `SetActionWithExceptionHandling` owns the catch
  ladder that turns exceptions into exit codes.
- **Error streams**: human-mode errors go to stderr through the interaction
  service's stderr-bound `IAnsiConsole`; machine-mode (`--format json`)
  errors are plain text on stderr too, never mixed into the JSON on stdout.
- **`update`'s exit code**: `update` returns `ExitCodeConstants.Success` even
  when some skills fail their update check or updates are found but not
  applied; a non-zero exit is reserved for the command itself failing to run,
  not for what it reports.
- **Services**: resolve everything the handler needs from `ICommandServices`
  inside `ExecuteAsync`. Commands take no constructor dependencies.
- **DI-constructor exception**: the parameterless-constructor rule above
  applies to the five `{Verb}Command` leaves only. The two composition roots,
  `SkillsCommand` (public, embeddable) and `SkillsRootCommand` (internal,
  standalone), each take `IServiceProvider` and are the deliberate seam where
  DI enters the tree: they wrap it in `ICommandServices` and expose it through
  `ICommandServicesSource`, which `CommandServicesResolver` walks
  `Symbol.Parents` to find from any leaf command's `ExecuteAsync`. Do not add
  a third way to get services into a command; a new composition root follows
  this same constructor shape instead.

```csharp
namespace Skills.Commands;

internal sealed class GreetCommand : Command
{
    public GreetCommand() : base("greet", "Print a greeting.")
    {
        Arguments.Add(Opt<NameArgument>.Instance);
        Options.Add(Opt<LoudOption>.Instance);

        this.AddExamples("greet world", "greet world --loud");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var interaction = services.GetRequiredService<IInteractionService>();

        var name = parseResult.GetValue(Opt<NameArgument>.Instance);
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ExitException($"Missing required argument '{NameArgument.ArgumentName}'.");
        }

        var loud = parseResult.GetValue(Opt<LoudOption>.Instance);
        interaction.WriteLine(loud ? $"HELLO, {name.ToUpperInvariant()}!" : $"Hello, {name}.");

        await Task.CompletedTask;
        return ExitCodeConstants.Success;
    }
}
```
