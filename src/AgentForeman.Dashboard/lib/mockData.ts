import type {
  Mission,
  MissionEvent,
  MissionLogLink,
  SummaryMetric,
  SystemCheck,
} from "@/lib/types";

export const dashboardSummary: SummaryMetric[] = [
  {
    id: "active",
    label: "Active missions",
    value: "3",
    description: "Currently flowing through planning, coding, or verification.",
    tone: "active",
  },
  {
    id: "paused",
    label: "Paused missions",
    value: "1",
    description: "Waiting for quota recovery before execution can continue.",
    tone: "warning",
  },
  {
    id: "failed",
    label: "Failed missions",
    value: "1",
    description: "Require human inspection before another repair attempt.",
    tone: "danger",
  },
  {
    id: "review",
    label: "PRs awaiting review",
    value: "1",
    description: "Work is ready for a human to review and decide next action.",
    tone: "review",
  },
  {
    id: "completed",
    label: "Completed missions",
    value: "1",
    description: "Closed out after the branch, tests, and pull request lifecycle.",
    tone: "complete",
  },
];

export const missions: Mission[] = [
  {
    id: "github-17",
    workItemId: "17",
    title: "Tighten campaign creative validation for duplicated assets",
    status: "Planning",
    statusNote:
      "Claude is building the plan. No code has been written yet, but the work item context is already loaded.",
    operatorSignal:
      "Healthy early-stage flow. Watch for plan quality, not repository state.",
    branch: "agent/issue-17",
    createdAt: "2026-05-23 09:12",
    updatedAt: "2026-05-23 09:18",
  },
  {
    id: "github-18",
    workItemId: "18",
    title: "Harden branch preparation diagnostics for WSL line-ending drift",
    status: "Coding",
    statusNote:
      "Codex is editing the repo now. The current branch is active and logs are still growing.",
    operatorSignal:
      "Mission is in motion. Intervention is unnecessary unless execution stalls.",
    branch: "agent/issue-18",
    createdAt: "2026-05-23 08:40",
    updatedAt: "2026-05-23 09:23",
  },
  {
    id: "github-19",
    workItemId: "19",
    title: "Add API-backed mission event summaries for dashboard consumption",
    status: "Testing",
    statusNote:
      "Implementation finished and verification is underway against the mission test suite.",
    operatorSignal:
      "Pipeline is healthy but fragile; a failed test here usually triggers a repair loop.",
    branch: "agent/issue-19",
    createdAt: "2026-05-23 07:50",
    updatedAt: "2026-05-23 09:11",
  },
  {
    id: "github-20",
    workItemId: "20",
    title: "Retry quota-exhausted mission after provider cool-down",
    status: "PausedQuota",
    statusNote:
      "The mission paused itself after a quota signal from the coding provider. Work is safe but blocked until the retry window.",
    operatorSignal:
      "No code problem yet. The right action is patience unless the retry window looks wrong.",
    branch: "agent/issue-20",
    retryAfter: "2026-05-23 11:30",
    createdAt: "2026-05-23 06:55",
    updatedAt: "2026-05-23 09:05",
  },
  {
    id: "github-21",
    workItemId: "21",
    title: "Repair failing system health projection after schema drift",
    status: "Failed",
    statusNote:
      "Verification failed twice and the latest repair attempt did not stabilize the build.",
    operatorSignal:
      "High attention. This mission is no longer self-healing and likely needs a human decision.",
    branch: "agent/issue-21",
    lastError:
      "Build failed: SystemStatusProjectionTests expects mission_events schema initializer.",
    createdAt: "2026-05-23 05:42",
    updatedAt: "2026-05-23 08:58",
  },
  {
    id: "github-22",
    workItemId: "22",
    title: "Expose dashboard summary endpoints for frontend integration",
    status: "PullRequestCreated",
    statusNote:
      "Implementation and verification are done. The mission is waiting for human review on the pull request.",
    operatorSignal:
      "Good candidate for triage. Agent work is complete; the bottleneck is reviewer attention.",
    branch: "agent/issue-22",
    pullRequestUrl: "https://github.com/caioh/agent-foreman/pull/22",
    createdAt: "2026-05-22 19:10",
    updatedAt: "2026-05-23 08:22",
  },
  {
    id: "github-23",
    workItemId: "23",
    title: "Synchronize completed mission labels with closed GitHub issues",
    status: "Completed",
    statusNote:
      "Mission has been verified, submitted, and reconciled with the closed work item state.",
    operatorSignal:
      "No action needed. Keep for historical audit and operator confidence.",
    branch: "agent/issue-23",
    pullRequestUrl: "https://github.com/caioh/agent-foreman/pull/23",
    createdAt: "2026-05-22 16:25",
    updatedAt: "2026-05-23 07:44",
  },
];

export const reviewQueue = missions.filter(
  (mission) => mission.status === "PullRequestCreated",
);

export const systemChecks: SystemCheck[] = [
  {
    id: "database",
    name: "Database",
    kind: "State store",
    status: "OK",
    summary: "PostgreSQL state store reachable.",
    detail:
      "Mission, event, and provider-state tables are assumed available for the current local environment.",
  },
  {
    id: "git",
    name: "Git",
    kind: "Workspace tooling",
    status: "Warning",
    summary: "Repository cleanliness checks need operator awareness.",
    detail:
      "WSL and Windows line-ending drift can still surface as dirty worktrees on /mnt/c checkouts.",
  },
  {
    id: "github-cli",
    name: "GitHub CLI",
    kind: "Provider tool",
    status: "OK",
    summary: "Available for work item and pull request flows.",
    detail:
      "Mocked status assumes authentication and repo access are healthy in the local environment.",
  },
  {
    id: "claude",
    name: "Claude",
    kind: "Planning provider",
    status: "OK",
    summary: "Planning provider assumed reachable.",
    detail:
      "Plans can be generated, but the dashboard should later display quota and provider-state timing explicitly.",
  },
  {
    id: "codex",
    name: "Codex",
    kind: "Coding provider",
    status: "Warning",
    summary: "Available, with quota sensitivity.",
    detail:
      "This mock state represents a provider that works normally but can pause missions during heavy usage windows.",
  },
  {
    id: "config",
    name: "Config",
    kind: "Bootstrap",
    status: "OK",
    summary: "agent-foreman.yaml resolved.",
    detail:
      "The frontend assumes local configuration resolves correctly through the existing config loader rules.",
  },
  {
    id: "daemon",
    name: "Daemon",
    kind: "Scheduler",
    status: "Failed",
    summary: "Daemon not running in this mocked local environment.",
    detail:
      "Useful for surfacing the distinction between a healthy toolchain and a stopped background mission loop.",
  },
];

const missionEvents: Record<string, MissionEvent[]> = {
  "github-17": [
    {
      id: "e-17-1",
      missionId: "github-17",
      type: "MissionStarted",
      summary: "Mission queued from GitHub issue",
      detail: "The work item was selected from the ready queue and a local mission record was created.",
      occurredAt: "2026-05-23 09:12",
      level: "info",
    },
    {
      id: "e-17-2",
      missionId: "github-17",
      type: "BranchPrepared",
      summary: "Mission branch reserved",
      detail: "Branch metadata is staged and the planner now has a stable execution context.",
      occurredAt: "2026-05-23 09:14",
      level: "success",
    },
    {
      id: "e-17-3",
      missionId: "github-17",
      type: "PlanningStarted",
      summary: "Claude plan requested",
      detail: "The planner is reading repo context and producing a task-oriented plan file.",
      occurredAt: "2026-05-23 09:18",
      level: "info",
    },
  ],
  "github-18": [
    {
      id: "e-18-1",
      missionId: "github-18",
      type: "MissionStarted",
      summary: "Mission resumed into coding",
      detail: "A previous failed mission re-entered the coding stage after a fresh operator retry.",
      occurredAt: "2026-05-23 08:54",
      level: "info",
    },
    {
      id: "e-18-2",
      missionId: "github-18",
      type: "BranchPrepared",
      summary: "Agent branch restored cleanly",
      detail: "The branch-preparation step completed and the repo is ready for code edits.",
      occurredAt: "2026-05-23 08:56",
      level: "success",
    },
    {
      id: "e-18-3",
      missionId: "github-18",
      type: "PlanningStarted",
      summary: "Existing plan loaded",
      detail: "The mission reused an existing saved plan to avoid unnecessary replanning.",
      occurredAt: "2026-05-23 08:58",
      level: "info",
    },
    {
      id: "e-18-4",
      missionId: "github-18",
      type: "PlanningCompleted",
      summary: "Plan validated for execution",
      detail: "Inputs were checked and the agent advanced to code changes.",
      occurredAt: "2026-05-23 08:59",
      level: "success",
    },
    {
      id: "e-18-5",
      missionId: "github-18",
      type: "ExecutionStarted",
      summary: "Codex editing repository",
      detail: "Implementation is active and logs are expected to grow until verification begins.",
      occurredAt: "2026-05-23 09:03",
      level: "info",
    },
  ],
  "github-19": [
    {
      id: "e-19-1",
      missionId: "github-19",
      type: "MissionStarted",
      summary: "Mission promoted to verification",
      detail: "The coding phase completed with a candidate diff ready for tests.",
      occurredAt: "2026-05-23 08:37",
      level: "info",
    },
    {
      id: "e-19-2",
      missionId: "github-19",
      type: "VerificationCompleted",
      summary: "Smoke verification still running",
      detail: "This mock mission stays in Testing to represent an in-flight verification stage.",
      occurredAt: "2026-05-23 09:11",
      level: "success",
    },
  ],
  "github-20": [
    {
      id: "e-20-1",
      missionId: "github-20",
      type: "MissionStarted",
      summary: "Mission resumed from queue",
      detail: "The work item moved from ready to active execution with a valid branch.",
      occurredAt: "2026-05-23 06:55",
      level: "info",
    },
    {
      id: "e-20-2",
      missionId: "github-20",
      type: "ExecutionStarted",
      summary: "Execution began normally",
      detail: "Codex started the change set with no repo or planning errors.",
      occurredAt: "2026-05-23 07:08",
      level: "info",
    },
    {
      id: "e-20-3",
      missionId: "github-20",
      type: "VerificationFailed",
      summary: "Provider quota interruption detected",
      detail: "The coding provider reported a quota limit, so the mission paused itself instead of continuing unsafely.",
      occurredAt: "2026-05-23 09:05",
      level: "warning",
    },
  ],
  "github-21": [
    {
      id: "e-21-1",
      missionId: "github-21",
      type: "MissionStarted",
      summary: "Mission entered repair loop",
      detail: "Initial verification exposed a schema mismatch and the mission was automatically routed into repair.",
      occurredAt: "2026-05-23 05:42",
      level: "info",
    },
    {
      id: "e-21-2",
      missionId: "github-21",
      type: "VerificationFailed",
      summary: "Schema expectation mismatch",
      detail: "Tests failed because the health projection expected a mission_events initializer path that was missing.",
      occurredAt: "2026-05-23 07:21",
      level: "error",
    },
    {
      id: "e-21-3",
      missionId: "github-21",
      type: "RepairStarted",
      summary: "Automatic repair attempt started",
      detail: "The mission generated a follow-up patch based on the failing test output.",
      occurredAt: "2026-05-23 07:38",
      level: "warning",
    },
    {
      id: "e-21-4",
      missionId: "github-21",
      type: "RepairCompleted",
      summary: "Repair patch applied",
      detail: "A candidate fix landed, but the result still needed verification.",
      occurredAt: "2026-05-23 08:03",
      level: "success",
    },
    {
      id: "e-21-5",
      missionId: "github-21",
      type: "VerificationFailed",
      summary: "Repair did not stabilize the mission",
      detail: "The build continued to fail, so the mission was marked Failed instead of looping forever.",
      occurredAt: "2026-05-23 08:58",
      level: "error",
    },
  ],
  "github-22": [
    {
      id: "e-22-1",
      missionId: "github-22",
      type: "MissionStarted",
      summary: "Mission created from ready issue",
      detail: "The API endpoint task was accepted and recorded in mission state.",
      occurredAt: "2026-05-22 19:10",
      level: "info",
    },
    {
      id: "e-22-2",
      missionId: "github-22",
      type: "BranchPrepared",
      summary: "Branch established",
      detail: "A clean branch was created to hold the API feature work.",
      occurredAt: "2026-05-22 19:16",
      level: "success",
    },
    {
      id: "e-22-3",
      missionId: "github-22",
      type: "PlanningStarted",
      summary: "Plan generated",
      detail: "A concrete implementation plan was saved before coding.",
      occurredAt: "2026-05-22 19:20",
      level: "info",
    },
    {
      id: "e-22-4",
      missionId: "github-22",
      type: "PlanningCompleted",
      summary: "Execution approved",
      detail: "The mission advanced from planning to implementation.",
      occurredAt: "2026-05-22 19:29",
      level: "success",
    },
    {
      id: "e-22-5",
      missionId: "github-22",
      type: "ExecutionStarted",
      summary: "Implementation entered code phase",
      detail: "The read-only API feature was coded and wired into the solution.",
      occurredAt: "2026-05-22 20:02",
      level: "info",
    },
    {
      id: "e-22-6",
      missionId: "github-22",
      type: "VerificationCompleted",
      summary: "Tests passed",
      detail: "The targeted tests and final suite completed successfully.",
      occurredAt: "2026-05-22 21:04",
      level: "success",
    },
    {
      id: "e-22-7",
      missionId: "github-22",
      type: "PullRequestCreated",
      summary: "Pull request opened",
      detail: "The work is now waiting for a human reviewer rather than another agent action.",
      occurredAt: "2026-05-22 21:22",
      level: "success",
    },
  ],
  "github-23": [
    {
      id: "e-23-1",
      missionId: "github-23",
      type: "MissionStarted",
      summary: "Mission entered sync flow",
      detail: "The work item synchronization pass began with label cleanup rules loaded.",
      occurredAt: "2026-05-22 16:25",
      level: "info",
    },
    {
      id: "e-23-2",
      missionId: "github-23",
      type: "VerificationCompleted",
      summary: "Sync behavior verified",
      detail: "The implementation was validated with fake repositories and providers.",
      occurredAt: "2026-05-22 17:44",
      level: "success",
    },
    {
      id: "e-23-3",
      missionId: "github-23",
      type: "PullRequestCreated",
      summary: "Audit trail preserved in pull request",
      detail: "The reviewable change landed and the mission later synchronized to Completed.",
      occurredAt: "2026-05-22 18:10",
      level: "success",
    },
  ],
};

const missionLogs: Record<string, MissionLogLink[]> = Object.fromEntries(
  missions.map((mission) => [
    mission.id,
    [
      {
        label: "plan.md",
        path: `${mission.branch}/.agent/runs/issue-${mission.workItemId}/plan.md`,
        kind: "Document",
      },
      {
        label: "claude-plan.log",
        path: `${mission.branch}/.agent/runs/issue-${mission.workItemId}/claude-plan.log`,
        kind: "Log",
      },
      {
        label: "codex-exec.log",
        path: `${mission.branch}/.agent/runs/issue-${mission.workItemId}/codex-exec.log`,
        kind: "Log",
      },
      {
        label: "tests.log",
        path: `${mission.branch}/.agent/runs/issue-${mission.workItemId}/tests.log`,
        kind: "Log",
      },
      {
        label: "repair-attempt-1.log",
        path: `${mission.branch}/.agent/runs/issue-${mission.workItemId}/repair-attempt-1.log`,
        kind: "Log",
      },
    ],
  ]),
);

export function getMissionById(id: string) {
  return missions.find((mission) => mission.id === id);
}

export function getMissionEvents(id: string) {
  return missionEvents[id] ?? [];
}

export function getMissionLogs(id: string) {
  return missionLogs[id] ?? [];
}
