# CLI layer conventions

`src/Skills/Commands/` follows the System.CommandLine shape from Nitro
(`src/Nitro/CommandLine/src/CommandLine` in ChilliCream/graphql-platform). Diff
against that repo when in doubt; this file is the local summary, not a
substitute.

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
- **Errors**: throw `ExitException` (via `ThrowHelper` where a factory
  already exists) for CLI-layer aborts; an empty-message `ExitException` means
  the command already reported detail through `IInteractionService`. Throw
  `CliException` for domain errors that carry an `ExitCode` plus optional
  `Title`/`Hint` rendered as an error panel. Return codes from
  `ExitCodeConstants` (`Success`, `Failure`, `Cancelled`). Never call
  `Environment.Exit`; `SetActionWithExceptionHandling` owns the catch ladder
  that turns exceptions into exit codes.
- **Services**: resolve everything the handler needs from `ICommandServices`
  inside `ExecuteAsync`. Commands take no constructor dependencies.

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
            throw ThrowHelper.MissingRequiredArgument(NameArgument.ArgumentName);
        }

        var loud = parseResult.GetValue(Opt<LoudOption>.Instance);
        interaction.WriteLine(loud ? $"HELLO, {name.ToUpperInvariant()}!" : $"Hello, {name}.");

        await Task.CompletedTask;
        return ExitCodeConstants.Success;
    }
}
```
