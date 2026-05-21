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

## Scope Guardrails

Do not add external integrations until the corresponding command behavior is being implemented. The core project should own mission concepts and provider abstractions; infrastructure should own concrete adapters such as GitHub CLI, Claude CLI, Codex CLI, SQLite, and process execution.
