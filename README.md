# Agent Foreman

Agent Foreman is a CLI-first mission control system for AI coding agents.

The first version is intentionally small. It will coordinate one ready GitHub issue through planning, implementation, tests, pull request creation, logging, state persistence, and quota-aware pause/resume behavior. This bootstrap only creates the repository structure and a minimal `agent-foreman` help command.

## Current Status

Implemented now:

- `agent-foreman` with no arguments
- `agent-foreman help`
- `agent-foreman --help`
- `agent-foreman -h`

Each command prints the same help text.

Future commands listed in help but not implemented yet:

- `run-once`
- `daemon`
- `status`
- `resume`
- `cancel`
- `doctor`

## Repository Layout

```text
src/
  AgentForeman.Api/
  AgentForeman.Cli/
  AgentForeman.Core/
  AgentForeman.Infrastructure/

tests/
  AgentForeman.Tests/
```

## Development

Build and test:

```bash
dotnet test AgentForeman.sln
```

Run the CLI:

```bash
dotnet run --project src/AgentForeman.Cli -- help
```

Run the API:

```bash
dotnet run --project src/AgentForeman.Api -- --config agent-foreman.yaml
```

Run the dashboard:

```bash
cd src/AgentForeman.Dashboard
npm install
npm run dev
```

To connect the dashboard to the API, set `AGENT_FOREMAN_API_BASE_URL`.
Example:

```bash
AGENT_FOREMAN_API_BASE_URL=http://localhost:52888 npm run dev
```

## WSL Repo Policy

If Agent Foreman runs inside WSL, prefer a WSL-native `project.repoPath` such as `~/src/project` instead of `/mnt/c/...`.

To avoid dirty worktrees caused only by line-ending normalization, keep target repositories on `LF` and commit a `.gitattributes` policy such as:

```gitattributes
* text=auto
*.cs text eol=lf
*.csproj text eol=lf
*.sln text eol=lf
*.md text eol=lf
*.yml text eol=lf
*.yaml text eol=lf
*.json text eol=lf
*.sh text eol=lf
```

## Scope Guardrails

Do not add external integrations until the corresponding command behavior is being implemented. The core project should own mission concepts and provider abstractions; infrastructure should own concrete adapters such as GitHub CLI, Claude CLI, Codex CLI, SQLite, and process execution.
