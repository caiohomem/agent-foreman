# Agent Foreman Dashboard Frontend Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a standalone read-only Next.js dashboard shell in `src/AgentForeman.Dashboard` with mocked mission and system data.

**Architecture:** The frontend lives as an isolated Next.js app under `src/AgentForeman.Dashboard` and does not participate in the .NET solution or backend build. App Router pages render local mock data through small, focused presentational components that emphasize mission status clarity.

**Tech Stack:** Next.js, React, TypeScript, Tailwind CSS, npm

---

### Task 1: Scaffold The Standalone Dashboard App

**Files:**
- Create: `src/AgentForeman.Dashboard/*`
- Modify: `README.md`

- [ ] **Step 1: Scaffold the app**

Run: `cd src && npm create next-app@latest AgentForeman.Dashboard -- --ts --tailwind --app --eslint --use-npm --src-dir false --import-alias "@/*"`
Expected: a new `src/AgentForeman.Dashboard` app with `package.json`, `app/`, and Tailwind support.

- [ ] **Step 2: Remove unneeded starter assets and defaults**

Replace the starter home page and branding files with project-specific placeholders so the app begins as an Agent Foreman shell instead of a generic Next template.

- [ ] **Step 3: Update root documentation**

Add dashboard run instructions to `README.md`:

```bash
cd src/AgentForeman.Dashboard
npm install
npm run dev
```

### Task 2: Define Mock Types And Seed Data

**Files:**
- Create: `src/AgentForeman.Dashboard/lib/types.ts`
- Create: `src/AgentForeman.Dashboard/lib/mockData.ts`

- [ ] **Step 1: Write the local types**

Define typed local models for missions, events, and system checks with a mission status union that includes the required statuses.

- [ ] **Step 2: Add realistic mock records**

Create summary counts, a missions list, mission detail records, timeline events, log placeholders, and system checks that cover normal flow, paused quota, failure, review, and completion scenarios.

### Task 3: Build The Shared App Shell

**Files:**
- Create: `src/AgentForeman.Dashboard/components/app-shell.tsx`
- Create: `src/AgentForeman.Dashboard/components/sidebar.tsx`
- Modify: `src/AgentForeman.Dashboard/app/layout.tsx`
- Modify: `src/AgentForeman.Dashboard/app/globals.css`

- [ ] **Step 1: Create the shell and sidebar**

Implement a desktop-first shell with left navigation and a top header showing `Agent Foreman` and `Environment: Local`.

- [ ] **Step 2: Establish the visual system**

Use CSS variables and Tailwind utility composition to create a clean technical dashboard aesthetic with cards, table surfaces, and monospace treatment for operational identifiers.

### Task 4: Build Reusable Mission UI Components

**Files:**
- Create: `src/AgentForeman.Dashboard/components/status-badge.tsx`
- Create: `src/AgentForeman.Dashboard/components/summary-card.tsx`
- Create: `src/AgentForeman.Dashboard/components/mission-table.tsx`
- Create: `src/AgentForeman.Dashboard/components/mission-timeline.tsx`
- Create: `src/AgentForeman.Dashboard/components/log-links.tsx`

- [ ] **Step 1: Implement status-aware components**

Create badge variants that clearly distinguish active flow, warnings, failures, review waiting, and completed states.

- [ ] **Step 2: Build the table, timeline, and log components**

Keep the components read-only and focused on operational clarity.

### Task 5: Implement The Dashboard Pages

**Files:**
- Modify: `src/AgentForeman.Dashboard/app/page.tsx`
- Create: `src/AgentForeman.Dashboard/app/missions/page.tsx`
- Create: `src/AgentForeman.Dashboard/app/missions/[id]/page.tsx`
- Create: `src/AgentForeman.Dashboard/app/system/page.tsx`

- [ ] **Step 1: Build the home page**

Render the five summary cards and product framing text.

- [ ] **Step 2: Build the missions page**

Render the read-only mission table using mocked mission data.

- [ ] **Step 3: Build the mission details page**

Render mission summary fields, timeline events, and log placeholders. Include a simple not-found state when the route id does not match any mock mission.

- [ ] **Step 4: Build the system page**

Render system check cards with `OK`, `Warning`, and `Failed` states.

### Task 6: Verify Production Build

**Files:**
- Verify: `src/AgentForeman.Dashboard`

- [ ] **Step 1: Install frontend dependencies**

Run: `cd src/AgentForeman.Dashboard && npm install`
Expected: dependencies install successfully with npm only.

- [ ] **Step 2: Run the production build**

Run: `cd src/AgentForeman.Dashboard && npm run build`
Expected: Next.js production build completes without errors.

- [ ] **Step 3: Review diff**

Run: `git diff -- src/AgentForeman.Dashboard README.md`
Expected: diff is limited to the standalone dashboard app and documentation.
