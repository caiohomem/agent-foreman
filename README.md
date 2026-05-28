# Agent Foreman

Agent Foreman is a mission control system for AI coding agents.

It coordinates a GitHub issue through planning, implementation, verification, pull request creation, logging, persistent mission state, quota-aware pause/resume, mission events, and run summaries that can later feed RAG memory.

## Current Status

Implemented now:

- CLI orchestration for one ready GitHub issue.
- Claude CLI planning into `plan.md`.
- Codex CLI execution into `codex-exec.log`.
- Configured test execution into `tests.log`.
- Git branch preparation, commit, push, and pull request creation.
- PostgreSQL state store for missions, events, provider state, and run summaries.
- Quota/rate-limit detection with `PausedQuota` state and retry timing.
- Mission resume, cancel, sync, status, event inspection, and summary generation.
- Read-only ASP.NET Core Minimal API for dashboard data.
- Standalone Next.js dashboard frontend for mission monitoring.

Not implemented yet:

- Authentication.
- SignalR/live updates.
- Write actions from the dashboard.
- Embeddings or pgvector.
- Deploy providers.
- RAG retrieval.
- MCP logic.
- Azure DevOps.
- SSP/DSP/OpenRTB systems.

## Repository Layout

```text
src/
  AgentForeman.Api/          ASP.NET Core Minimal API for read-only dashboard data
  AgentForeman.Cli/          CLI entry point and command routing
  AgentForeman.Core/         Core contracts, mission model, provider abstractions
  AgentForeman.Dashboard/    Standalone Next.js dashboard
  AgentForeman.Infrastructure/
                              GitHub CLI, Claude CLI, Codex CLI, Git, PostgreSQL,
                              process execution, orchestration, summaries

tests/
  AgentForeman.Tests/
```

## Configuration

The CLI and API load configuration from `agent-foreman.yaml` by default.

Most commands also accept:

```bash
--config path/to/agent-foreman.yaml
```

For the API, a config path can also be provided through:

```bash
AGENT_FOREMAN_CONFIG=/path/to/agent-foreman.yaml
```

## CLI

Run help:

```bash
dotnet run --project src/AgentForeman.Cli -- help
```

Common commands:

```bash
dotnet run --project src/AgentForeman.Cli -- doctor
dotnet run --project src/AgentForeman.Cli -- config validate
dotnet run --project src/AgentForeman.Cli -- state status
dotnet run --project src/AgentForeman.Cli -- work-items ready
dotnet run --project src/AgentForeman.Cli -- run-once
dotnet run --project src/AgentForeman.Cli -- daemon --once
dotnet run --project src/AgentForeman.Cli -- status
dotnet run --project src/AgentForeman.Cli -- events github-24
dotnet run --project src/AgentForeman.Cli -- summarize 24
dotnet run --project src/AgentForeman.Cli -- resume github-24 --force
dotnet run --project src/AgentForeman.Cli -- cancel github-24 "reason"
dotnet run --project src/AgentForeman.Cli -- sync --dry-run
```

Stage commands are also available when running the pipeline manually:

```bash
dotnet run --project src/AgentForeman.Cli -- plan 24
dotnet run --project src/AgentForeman.Cli -- execute 24
dotnet run --project src/AgentForeman.Cli -- verify 24
dotnet run --project src/AgentForeman.Cli -- submit 24
```

## Mission Artifacts

Mission run files are written under the configured project repository:

```text
.agent/runs/issue-{id}/
  plan.md
  claude-plan.log
  codex-exec.log
  tests.log
  summary.md
  failure-summary.md
  resume-context.md
  summary-context-successsummary.md
  summary-context-failuresummary.md
  summary-context-resumecontext.md
```

`summary.md` is generated for successful handoff states.

`failure-summary.md` and `resume-context.md` are generated for failed runs.

`resume-context.md` is also generated for quota pauses when useful.

The `summary-context-*` files are intermediate Claude context files kept inside the mission run directory so the summary generator does not pass oversized prompts through CLI arguments.

## API

Run the API:

```bash
dotnet run --project src/AgentForeman.Api -- --config agent-foreman.yaml
```

The development launch profile listens on:

```text
http://localhost:52888
https://localhost:52887
```

Read-only endpoints:

```text
GET /api/health
GET /api/dashboard/summary
GET /api/missions?status=&limit=
GET /api/missions/{id}
GET /api/missions/{id}/events?limit=
GET /api/missions/{id}/summaries
```

Swagger/OpenAPI is enabled in development.

If the API is launched from Visual Studio and cannot find `agent-foreman.yaml`, set `AGENT_FOREMAN_CONFIG` in the launch profile to the absolute config path.

## Dashboard

Run the dashboard:

```bash
cd src/AgentForeman.Dashboard
npm install
npm run dev
```

The dashboard reads from the API by default:

```text
http://localhost:52888
```

Override the API base URL with:

```bash
AGENT_FOREMAN_API_BASE_URL=http://localhost:52888 npm run dev
```

If the API is unavailable, the dashboard falls back to local mock data so the UI still loads.

The dashboard is read-only. It shows:

- Dashboard mission counts.
- Mission table.
- Mission detail metadata.
- Mission event timeline.
- Generated logs and document placeholders.
- Saved run summaries.
- Read-only system status cards.

## Development

Build and test .NET projects:

```bash
dotnet test AgentForeman.sln
```

Build the dashboard:

```bash
cd src/AgentForeman.Dashboard
npm run build
```

Run the dashboard adapter test:

```bash
cd src/AgentForeman.Dashboard
node --import tsx --test lib/dashboard-data.test.ts
```

## WSL Repo Policy

If Agent Foreman runs inside WSL, prefer a WSL-native `project.repoPath` such as `~/src/project` instead of `/mnt/c/...`.

When a target repository is on `/mnt/c`, Windows and WSL Git line-ending settings can make the WSL worktree appear dirty even when Windows tooling looks clean. Agent Foreman checks the exact `project.repoPath` from WSL, so branch switching can be blocked by line-ending-only changes.

To reduce that risk, keep target repositories on `LF` and commit a `.gitattributes` policy such as:

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

Recommended Git settings inside WSL:

```bash
git config --global core.autocrlf input
git config --global core.eol lf
```

## Troubleshooting

If `resume --force` still fails with a dirty worktree, `--force` only retries a failed or paused mission. It does not bypass repository cleanliness checks. Inspect the exact target repo path:

```bash
git -C /path/from/project.repoPath status --short
git -C /path/from/project.repoPath diff --check
git -C /path/from/project.repoPath ls-files --eol
```

If WSL reports `fork: Resource temporarily unavailable` or `Out of memory`, stop stale Node/Next workers before retrying:

```bash
pkill -f "next"
pkill -f "postcss.js"
pkill -f "node .*jest-worker"
pkill -f "eslint.js"
```

If the system is still resource constrained, run from Windows PowerShell:

```powershell
wsl --shutdown
```

## Scope Guardrails

Keep changes small and tied to the current mission workflow.

Core owns mission concepts and provider abstractions. Infrastructure owns concrete adapters such as GitHub CLI, Claude CLI, Codex CLI, Git, PostgreSQL, and process execution.

Do not commit secrets, modify `.env` files, merge pull requests automatically, or add future systems before their command behavior is being implemented.
