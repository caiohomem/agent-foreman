# Agent Foreman Agent Guide

## Product Scope

Agent Foreman coordinates software development missions:

GitHub Issue -> Claude CLI plan -> Codex CLI implementation -> tests/build -> pull request -> logs and mission state -> quota-aware pause/resume.

## Current Bootstrap Scope

Only the minimal CLI help behavior is implemented. Keep the first version small:

- Load config from `agent-foreman.yaml`
- Read one GitHub issue with label `agent-ready`
- Create a branch
- Ask Claude CLI for `plan.md`
- Ask Codex CLI to implement
- Run tests from config
- Create a PR
- Save logs and mission state
- Detect quota/rate limits and pause

Do not implement unrelated future systems.

## Architectural Rules

- Create abstractions for providers before adding concrete provider integrations.
- Do not couple core logic directly to GitHub.
- Keep concrete GitHub CLI, Claude CLI, Codex CLI, process execution, and SQLite details in infrastructure.
- Do not implement Azure DevOps yet.
- Do not implement RAG yet.
- Do not implement a dashboard yet.
- Do not implement deploy providers yet.
- Do not implement SSP/DSP yet.
- Do not implement OpenRTB yet.

## Safety Rules

- Never commit secrets.
- Never merge pull requests automatically.
- Never modify `.env` files or production secrets.
- Prefer small, tested changes over broad scaffolding.

### Auto-merge override (opt-in)

`agent-foreman.yaml` exposes `safety.autoMergeAfterChecks` (default `false`). When set to `true`, the submit step enables GitHub auto-merge on the PR via `gh pr merge --auto --<method>`. This is the only path in the codebase that auto-merges PRs, and it is gated by the explicit opt-in flag. Use only when the agent's `verify` stage is sufficient to catch regressions, and review the resulting PR queue out of band.
