# Skills.Library

Implementation assembly for the [`skills`](https://www.nuget.org/packages/skills)
CLI, published so ChilliCream's own tooling can reference it directly.

If you want to manage AI agent skills, install the CLI instead:

```bash
dotnet tool install -g skills
```

Skills are markdown files (`SKILL.md`) with YAML frontmatter that teach AI
coding agents how to do something. The `skills` CLI installs them from GitHub,
GitLab, generic Git repos, or local paths, into whichever agents are present on
the machine.
