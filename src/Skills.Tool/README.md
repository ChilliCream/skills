# skills

A CLI for managing AI agent skills.

Skills are markdown files (`SKILL.md`) with YAML frontmatter that teach AI coding
agents how to do something. The CLI installs them from GitHub, GitLab, generic Git
repos, or local paths - into whichever agents you have on your machine.

## Install

The default NuGet package and command are `skills`. The `skills` package is
published as an equivalent package alias, and it installs the same `skills`
command.

### Run without installing (`dnx`)

If you have the **.NET 10 SDK** (or newer), `dnx` runs the tool one-shot - no
`PATH` shim, no global state:

```bash
dnx skills add chillicream/agent-skills
```

`dnx` is a shell script that ships with the SDK; it forwards to
`dotnet tool exec`, which downloads the package into the NuGet cache and runs
it. Subsequent runs hit the cache and are immediate.

**First run prompts:**

```
Tool package skills@1.0.0 will be downloaded from source
https://api.nuget.org/v3/index.json. Proceed? [y/n] (y):
```

Pass `--yes` to skip the prompt (useful in CI):

```bash
dnx --yes skills add anthropics/skills
```

**Pin a version** with `@`:

```bash
dnx skills@1.0.0 add anthropics/skills
dnx skills@1.* add anthropics/skills      # latest 1.x
```

**Allow prereleases:**

```bash
dnx --prerelease skills add anthropics/skills
```

**Use a custom feed:**

```bash
dnx --source https://my.feed/v3/index.json skills add ...
```

If a `.config/dotnet-tools.json` manifest is in scope, `dnx` uses the version
pinned there instead of the latest - handy for repo-local tool versions.

`dnx skills` and `dotnet tool exec skills` are equivalent; `dnx` is just the
shorter form.

### Persistent install

To put `skills` on your `PATH` for repeated use:

```bash
dotnet tool install -g skills
```

Requires the .NET SDK (8.0 or newer).

## Quick start

```bash
# Install all skills from a GitHub repo into Claude Code
dnx skills add chillicream/agent-skills --agent claude-code

# Pick specific skills interactively
dnx skills add chillicream/agent-skills

# Install one skill into multiple agents
dnx skills add chillicream/agent-skills --skill code-review --agent claude-code --agent cursor

# Install everything into every detected agent, no prompts
dnx skills add chillicream/agent-skills --all

# List what's installed
dnx skills list

# Update installed skills to the latest version
dnx skills update

# Remove a skill
dnx skills remove code-review

# Scaffold a new skill
dnx skills init my-skill
```

## Sources

`skills add <source>` understands several source forms:

| Form                                 | Example                                  |
|--------------------------------------|------------------------------------------|
| `owner/repo` (GitHub)                | `skills add anthropics/skills`           |
| Full Git URL                         | `skills add https://github.com/owner/repo` |
| GitLab project                       | `skills add gitlab:group/project`        |
| Local directory                      | `skills add ./my-skills`                 |

By default, the CLI performs a shallow clone for speed. Pass `--full-depth` to clone
full history.

## Scope: project vs. global

Without `--global`, skills are added to the current project (recorded in
`skills-lock.json` in the working directory). With `--global`, they're installed
once for your user under your XDG data dir (`$XDG_DATA_HOME/skills` or
`~/.local/share/skills`), with the global lock file (`.skill-lock.json`) kept
alongside the installed skills in that same directory.

## Agents

`skills` supports 55+ AI coding agents - Claude Code, Cursor, GitHub Copilot,
Codex, Continue, Gemini CLI, and many more. Detection is automatic; the
`--agent <name>` flag (repeatable) targets specific ones. Run `skills list` after
install to see which agents on your machine were updated.

By default skills are **symlinked** from a canonical location so editing one place
updates every agent. Pass `--copy` to copy files instead - useful for sandboxed
agents that don't follow symlinks.

## Authoring a skill

```bash
skills init my-skill
```

…creates `my-skill/SKILL.md` with the right frontmatter shape:

```markdown
---
name: my-skill
description: A brief description of what this skill does
---

# my-skill

Instructions for the agent to follow when this skill is activated.
```

Push the file to a public repo and anyone can install it with
`skills add <owner>/<repo>`.

## Commands

| Command                       | What it does                                       |
|-------------------------------|----------------------------------------------------|
| `skills add <source>`         | Install skill(s) from a source                     |
| `skills remove <skills...>`   | Uninstall skill(s)                                 |
| `skills list`                 | List installed skills                              |
| `skills update [skills...]`   | Update installed skills (alias `upgrade`, `check`) |
| `skills init [name]`          | Create a new `SKILL.md` scaffold                   |

Common flags: `--global / -g`, `--agent / -a <name>`, `--skill / -s <name>`,
`--yes / -y`, `--all`, `--copy`, `--full-depth`, `--list / -l`, `--json`.

`skills <command> --help` shows the full option list for each command.

## Source code

<https://github.com/chillicream/skills>

## License

MIT - see [LICENSE](https://github.com/chillicream/skills/blob/main/LICENSE).
