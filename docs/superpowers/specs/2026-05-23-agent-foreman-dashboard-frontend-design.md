# Agent Foreman Dashboard Frontend Design

**Goal:** Create the initial read-only Next.js dashboard frontend shell for monitoring Agent Foreman missions, system health, and mission details with mocked local data.

## Scope

This first frontend version is intentionally read-only.

Included:

- Separate Next.js app in `src/AgentForeman.Dashboard`
- Tailwind CSS styling
- App Router pages for `/`, `/missions`, `/missions/[id]`, and `/system`
- Sidebar and top header shell
- Mocked local data and local type definitions
- Mission-oriented UI that makes status easy to understand quickly

Explicitly excluded:

- Authentication
- Write actions such as resume, cancel, or run-once
- Live updates or SignalR
- Real API integration
- RAG, MCP, deploy providers, SSP/DSP logic

## Project Structure

The frontend remains fully separate from the .NET solution and backend build. The existing backend projects stay under `src/AgentForeman.*`. The new frontend lives under `src/AgentForeman.Dashboard` with its own `package.json`, `node_modules`, and Next.js build process.

Expected structure:

- `src/AgentForeman.Dashboard/app/`
- `src/AgentForeman.Dashboard/components/`
- `src/AgentForeman.Dashboard/lib/types.ts`
- `src/AgentForeman.Dashboard/lib/mockData.ts`

## UX Direction

The interface should feel like technical mission-control software rather than a marketing site. It should be clean, sharply organized, and readable under dense operational data.

Visual principles:

- Light, neutral base with strong card and section boundaries
- Clear hierarchy with deliberate spacing
- Monospace treatment for mission ids, work item ids, branches, and log paths
- Strong status badges so mission state is immediately scannable
- Detail surfaces that make a blocked, paused, failed, or review state understandable without clicking through multiple screens

The mission status presentation should be richer than a plain label. Each status row or detail panel should visually emphasize what stage the mission is in, whether intervention is likely needed, and whether the mission is flowing normally, paused, blocked by failure, or waiting for review.

## Navigation And Layout

### App Shell

Every page uses a shared shell with:

- Left sidebar:
  - Dashboard
  - Missions
  - System
- Top header:
  - Agent Foreman
  - Environment: Local

The layout is optimized for desktop first, with enough responsiveness to remain usable on narrower screens.

## Page Designs

### Home `/`

The home page is a high-signal summary view.

Content:

- Title: `Agent Foreman`
- Subtitle: `Mission control for AI coding agents.`
- Summary cards:
  - Active missions
  - Paused missions
  - Failed missions
  - PRs awaiting review
  - Completed missions

The cards should feel operational, not decorative. They should support quick scanning and include subtle supporting labels if needed.

### Missions `/missions`

The missions page displays a read-only missions table with mocked data.

Columns:

- Mission
- Work item
- Title
- Status
- Branch
- PR
- Updated

Example statuses included in the mock data:

- Planning
- Coding
- Testing
- PausedQuota
- Failed
- PullRequestCreated
- Completed

The status column is central to the page. Badges should communicate both state and severity, with a consistent visual mapping across the app.

### Mission Details `/missions/[id]`

The mission details page should feel like the operator’s diagnostic surface for one mission.

Summary section fields:

- Mission id
- Work item id
- Title
- Status
- Branch
- Pull request URL
- Retry after
- Last error
- Created at
- Updated at

Additional sections:

- Timeline with mocked events
- Log links/placeholders for generated artifacts

Timeline events:

- MissionStarted
- BranchPrepared
- PlanningStarted
- PlanningCompleted
- ExecutionStarted
- VerificationFailed
- RepairStarted
- RepairCompleted
- VerificationCompleted
- PullRequestCreated

Logs section:

- `plan.md`
- `claude-plan.log`
- `codex-exec.log`
- `tests.log`
- `repair-attempt-1.log`

### System `/system`

The system page is a read-only health board using mocked data.

Cards:

- Database
- Git
- GitHub CLI
- Claude
- Codex
- Config
- Daemon

Possible states:

- OK
- Warning
- Failed

## Component Boundaries

Recommended reusable components:

- `AppShell`
- `Sidebar`
- `StatusBadge`
- `SummaryCard`
- `MissionTable`
- `MissionTimeline`
- `LogLinks`

Each component should have one clear purpose and avoid early generalization.

## Data Model

All data remains local for now.

Files:

- `lib/types.ts`
- `lib/mockData.ts`

Types:

- `Mission`
- `MissionStatus`
- `MissionEvent`
- `SystemCheck`

The mock data should be realistic enough to demonstrate common operational states, especially paused quota, failed verification, review waiting, and successful completion.

## Integration Boundary

No real API calls are made in this version. The pages should import local mock data directly. The code should stay simple enough that later API integration can replace mock imports without redesigning the UI boundaries.

## Verification

Acceptance evidence for this feature:

- Next.js app exists under `src/AgentForeman.Dashboard`
- The home page renders
- The missions page renders the mission table
- The mission details page renders summary, events, and log placeholders
- The system page renders system status cards
- The app builds successfully with `npm run build`
- No write actions or backend integration are implemented
