using System.Collections.Immutable;
using System.Text.Json;
using Skills.Commands.List.Options;
using Skills.Extensions;
using Skills.Install;
using Skills.Interaction;
using Skills.Options;
using Skills.Paths;
using Skills.Skills;
using Skills.Utils;

namespace Skills.Commands;

internal sealed class ListCommand : Command
{
    public ListCommand() : base("list", "List installed skills")
    {
        Options.Add(Opt<GlobalOption>.Instance);
        Options.Add(Opt<AgentOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);
        Options.Add(Opt<JsonOption>.Instance);

        this.AddExamples("list", "list -g --json");

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
        var fileStore = services.GetRequiredService<IFileStore>();
        var systemEnvironment = services.GetRequiredService<ISystemEnvironment>();

        var global = parseResult.GetValue(Opt<GlobalOption>.Instance);
        var agents = parseResult.GetValue(Opt<AgentOption>.Instance) ?? [];

        AgentValidation.EnsureValidAgents(agents, registry.AgentTypes);

        var skills = CollectInstalledSkills(
            installer,
            registry,
            fileStore,
            systemEnvironment,
            agents,
            global,
            cancellationToken);

        if (!interaction.IsHumanReadable)
        {
            var payload = skills.Select(s => s.ToJsonType(global, registry)).ToArray();

            var json = JsonSerializer.Serialize(payload, JsonSourceGenerationContext.Default.InstalledSkillJsonArray);
            Console.WriteLine(json);
            return ExitCodeConstants.Success;
        }

        if (skills.Length == 0)
        {
            interaction.WriteDim(global ? "No global skills found." : "No project skills found.");
            if (!global)
            {
                interaction.WriteDim("Try listing global skills with -g");
            }
            return ExitCodeConstants.Success;
        }

        var scopeLabel = global ? "Global" : "Project";
        interaction.WriteMarkupLine($"[bold]{scopeLabel} Skills[/]");
        interaction.WriteLine();

        var grid = new Grid();
        grid.AddColumn(new GridColumn().PadRight(2));
        grid.AddColumn(new GridColumn().PadRight(2));
        grid.AddColumn(new GridColumn().PadRight(0));
        grid.AddRow("[grey66]Skill[/]", "[grey66]Path[/]", "[grey66]Agents[/]");

        foreach (var skill in skills)
        {
            var agentNames = skill.Agents.GetDisplayNames(registry).ToList();

            string agentCell;
            if (agentNames.Count == 0)
            {
                agentCell = "[yellow]not linked[/]";
            }
            else
            {
                var display =
                    agentNames.Count > 5
                        ? agentNames.Take(5).Join(", ") + $" +{agentNames.Count - 5} more"
                        : agentNames.Join(", ");
                agentCell = $"[dim]{Markup.Escape(display)}[/]";
            }

            grid.AddRow(
                $"[cyan]{Markup.Escape(skill.Name)}[/]",
                $"[dim]{Markup.Escape(SafePath.AbbreviateForDisplay(skill.CanonicalPath, systemEnvironment.HomeDirectory, systemEnvironment.CurrentDirectory))}[/]",
                agentCell);
        }

        interaction.WriteRenderable(grid);

        return ExitCodeConstants.Success;
    }

    private static ImmutableArray<InstalledSkill> CollectInstalledSkills(
        ISkillInstaller installer,
        AgentRegistry registry,
        IFileStore fileStore,
        ISystemEnvironment systemEnvironment,
        string[] agents,
        bool global,
        CancellationToken cancellationToken)
    {
        var cwd = systemEnvironment.CurrentDirectory;
        var agentFilter = agents.Length > 0 ? [.. agents] : registry.AgentTypes;
        var skills = new Dictionary<string, InstalledSkill>(StringComparer.Ordinal);
        var canonicalDir = installer.GetCanonicalSkillsDirectory(global, cwd);
        var canonicalDirFull = Path.GetFullPath(canonicalDir);

        if (fileStore.DirectoryExists(canonicalDir))
        {
            foreach (var entry in fileStore.EnumerateDirectories(canonicalDir))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var name = Path.GetFileName(entry);
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                var skillMd = Path.Combine(entry, KnownConfigNames.SkillFileName);
                if (!fileStore.FileExists(skillMd))
                {
                    continue;
                }

                skills[name] = new InstalledSkill(name, entry, []);
            }
        }

        foreach (var agentType in agentFilter)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!registry.TryGetConfig(agentType, out var config) || config is null)
            {
                continue;
            }

            if (global && config.GlobalSkillsDirectory is null)
            {
                continue;
            }

            var agentDir = installer.GetAgentBaseDirectory(agentType, global, cwd);
            var agentDirFull = Path.GetFullPath(agentDir);

            // Skip if this agent's dir IS the canonical dir (universal agents)
            if (string.Equals(agentDirFull, canonicalDirFull, SafePath.Comparison))
            {
                continue;
            }

            if (!fileStore.DirectoryExists(agentDir))
            {
                continue;
            }

            foreach (var entry in fileStore.EnumerateDirectories(agentDir))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var name = Path.GetFileName(entry);
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                var skillMd = Path.Combine(entry, KnownConfigNames.SkillFileName);
                if (!fileStore.FileExists(skillMd))
                {
                    continue;
                }

                if (!skills.TryGetValue(name, out var existing))
                {
                    existing = new InstalledSkill(name, entry, []);
                    skills[name] = existing;
                }

                if (!existing.Agents.Contains(agentType, StringComparer.Ordinal))
                {
                    existing.Agents.Add(agentType);
                }
            }
        }

        return [.. skills.Values];
    }

    internal sealed record InstalledSkill(string Name, string CanonicalPath, List<string> Agents);
}

internal sealed record InstalledSkillJson(string Name, string Path, string Scope, string[] Agents);

file static class Extensions
{
    public static InstalledSkillJson ToJsonType(
        this ListCommand.InstalledSkill skill,
        bool global,
        AgentRegistry registry)
    {
        return new InstalledSkillJson(
            skill.Name,
            skill.CanonicalPath,
            global ? "global" : "project",
            skill.Agents.GetDisplayNames(registry).ToArray());
    }

    public static IEnumerable<string> GetDisplayNames(this IEnumerable<string> agents, AgentRegistry registry)
        => agents.Select(a => registry.TryGetConfig(a, out var c) && c is not null ? c.DisplayName : a);
}
