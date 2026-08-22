using System.Collections.Immutable;
using Skills.Commands.Remove.Arguments;
using Skills.Commands.Remove.Options;
using Skills.Extensions;
using Skills.Install;
using Skills.Interaction;
using Skills.Locking;
using Skills.Options;
using Skills.Paths;
using Skills.Skills;
using Skills.Utils;

namespace Skills.Commands;

internal sealed class RemoveCommand : Command
{
    public RemoveCommand() : base("remove", "Remove installed skills")
    {
        Arguments.Add(Opt<SkillsArgument>.Instance);
        Options.Add(Opt<GlobalOption>.Instance);
        Options.Add(Opt<AgentOption>.Instance);
        Options.Add(Opt<YesOption>.Instance);
        Options.Add(Opt<AllOption>.Instance);

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var installer = services.GetRequiredService<ISkillInstaller>();
        var registry = services.GetRequiredService<AgentRegistry>();
        var interaction = services.GetRequiredService<IInteractionService>();
        var prompter = services.GetRequiredService<IRemoveCommandPrompter>();
        var projectLock = services.GetRequiredService<IProjectLockFile>();
        var globalLock = services.GetRequiredService<IGlobalLockFile>();
        var agentEnvironment = services.GetRequiredService<AgentEnvironment>();
        var fileStore = services.GetRequiredService<IFileStore>();
        var systemEnvironment = services.GetRequiredService<ISystemEnvironment>();
        var consoleEnvironment = services.GetRequiredService<ConsoleEnvironment>();

        var requestedSkills = parseResult.GetValue(Opt<SkillsArgument>.Instance) ?? [];
        var global = parseResult.GetValue(Opt<GlobalOption>.Instance);
        var agents = parseResult.GetValue(Opt<AgentOption>.Instance) ?? [];
        var yes = parseResult.GetValue(Opt<YesOption>.Instance);
        var all = parseResult.GetValue(Opt<AllOption>.Instance);

        if (agents.Length > 0)
        {
            var valid = registry.AgentTypes;
            var invalid = agents.Where(a => !valid.Contains(a)).ToList();
            if (invalid.Count > 0)
            {
                interaction.WriteError($"Invalid agents: {invalid.Join(", ")}");
                return ExitCodeConstants.Failure;
            }
        }

        var cwd = systemEnvironment.CurrentDirectory;
        var installed = CollectInstalledSkills(installer, registry, fileStore, cwd, global);

        if (installed.Length == 0)
        {
            interaction.WriteWarning("No skills found to remove.");
            return ExitCodeConstants.Success;
        }

        var nonInteractive =
            yes || all || consoleEnvironment.IsInputRedirected || agentEnvironment.IsRunningInsideAgent;

        var selected = ImmutableArray<string>.Empty;
        if (all)
        {
            selected = installed;
        }
        else if (requestedSkills.Length > 0)
        {
            selected = installed.Where(s => requestedSkills.Any(r => r.EqualsOrdinalIgnoreCase(s))).ToImmutableArray();

            if (selected.Length == 0)
            {
                interaction.WriteDim($"No matching skills found for: {requestedSkills.Join(", ")}");
                return ExitCodeConstants.Success;
            }
        }
        else if (nonInteractive)
        {
            interaction.WriteDim("No skills specified for removal.");
            return ExitCodeConstants.Success;
        }
        else
        {
            selected = await prompter.SelectSkillsAsync(installed, cancellationToken);
            if (selected.Length == 0)
            {
                interaction.WriteWarning("Removal cancelled");
                return ExitCodeConstants.Cancelled;
            }
        }

        var targetAgents = agents.Length > 0 ? agents.ToImmutableArray() : registry.AgentTypes;

        if (!nonInteractive)
        {
            var confirmed = await prompter.ConfirmRemovalAsync(selected, cancellationToken);
            if (!confirmed)
            {
                interaction.WriteWarning("Removal cancelled");
                return ExitCodeConstants.Cancelled;
            }
        }

        var failures = new List<(string Skill, string Error)>();
        var removed = 0;

        await interaction.StatusAsync("Removing skills...", async () =>
        {
            foreach (var skillName in selected)
            {
                try
                {
                    var canonicalPath = installer.GetCanonicalPath(skillName, global, cwd);

                    var deletedAnything = false;
                    foreach (var agentType in targetAgents)
                    {
                        var installPath = installer.GetInstallPath(skillName, agentType, global, cwd);
                        if (string.Equals(installPath, canonicalPath, SafePath.Comparison))
                        {
                            continue;
                        }

                        deletedAnything |= TryDeletePath(fileStore, installPath);
                    }

                    if (!IsCanonicalStillUsed(installer, registry, fileStore, skillName, global, cwd, targetAgents))
                    {
                        deletedAnything |= TryDeletePath(fileStore, canonicalPath);
                    }

                    var lockEntryRemoved = global
                        ? await globalLock.RemoveEntryAsync(skillName, cancellationToken)
                        : await projectLock.RemoveEntryAsync(skillName, cwd, cancellationToken);

                    // The discovered name may be an unsanitized on-disk folder that does not map to
                    // the sanitized install/canonical paths, so nothing on disk or in the lock was
                    // actually removed. Report that accurately instead of claiming success.
                    if (deletedAnything || lockEntryRemoved)
                    {
                        removed++;
                    }
                    else
                    {
                        failures.Add((skillName, "Nothing was removed (no matching files or lock entry found)."));
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    failures.Add((skillName, ex.Message));
                }
            }
        });

        if (removed > 0)
        {
            interaction.WriteSuccess($"Successfully removed {removed} skill(s)");
        }

        if (failures.Count > 0)
        {
            interaction.WriteError($"Failed to remove {failures.Count} skill(s)");
            foreach (var (skill, error) in failures)
            {
                interaction.WriteError($"  {skill}: {error}");
            }

            return ExitCodeConstants.Failure;
        }

        return ExitCodeConstants.Success;
    }

    private static ImmutableArray<string> CollectInstalledSkills(
        ISkillInstaller installer,
        AgentRegistry registry,
        IFileStore fileStore,
        string cwd,
        bool global)
    {
        var skills = new HashSet<string>(StringComparer.Ordinal);
        var directoriesToScan = new HashSet<string>(SafePath.Comparer)
        {
            installer.GetCanonicalSkillsDirectory(global, cwd)
        };

        foreach (var agentType in registry.AgentTypes)
        {
            var config = registry.GetConfig(agentType);
            if (global && config.GlobalSkillsDirectory is null)
            {
                continue;
            }

            directoriesToScan.Add(installer.GetAgentBaseDirectory(agentType, global, cwd));
        }

        foreach (var dir in directoriesToScan)
        {
            if (!fileStore.DirectoryExists(dir))
            {
                continue;
            }

            foreach (var entry in fileStore.EnumerateDirectories(dir))
            {
                skills.Add(Path.GetFileName(entry));
            }
        }

        return [.. skills.OrderBy(s => s, StringComparer.Ordinal)];
    }

    /// <summary>
    /// Returns <see langword="true"/> if at least one agent outside <paramref name="removedAgents"/>
    /// still has an install path for the skill, meaning the canonical directory must be kept.
    /// </summary>
    private static bool IsCanonicalStillUsed(
        ISkillInstaller installer,
        AgentRegistry registry,
        IFileStore fileStore,
        string skillName,
        bool global,
        string cwd,
        IReadOnlyList<string> removedAgents)
    {
        var removedSet = new HashSet<string>(removedAgents, StringComparer.Ordinal);
        foreach (var agentType in registry.AgentTypes)
        {
            if (removedSet.Contains(agentType))
            {
                continue;
            }

            var path = installer.GetInstallPath(skillName, agentType, global, cwd);
            if (fileStore.PathExists(path))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Deletes the path when it exists and reports whether something was actually removed.
    /// Returns <see langword="false"/> when nothing existed at <paramref name="path"/> or the
    /// best-effort deletion failed, so callers can report removal accurately.
    /// </summary>
    private static bool TryDeletePath(IFileStore fileStore, string path)
    {
        if (!fileStore.PathExists(path))
        {
            return false;
        }

        try
        {
            fileStore.DeletePath(path);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Best-effort
            return false;
        }
    }
}
