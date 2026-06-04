import type {
  ApiDashboardSummary,
  ApiMission,
  ApiMissionEvent,
  ApiRunSummary,
} from "./api";
import {
  dashboardSummary,
  getMissionById as getMockMissionById,
  getMissionEvents as getMockMissionEvents,
  getMissionLogs as getMockMissionLogs,
  missions as mockMissions,
  reviewQueue as mockReviewQueue,
  systemChecks,
} from "./mockData";
import {
  getDashboardSummary,
  getMission,
  getMissionEvents,
  getMissions,
  getMissionSummaries,
} from "./api";
import type {
  Mission,
  MissionEvent,
  MissionLogLink,
  MissionStatus,
  MissionSummary,
  SummaryMetric,
} from "./types";

export async function getDashboardOverview() {
  try {
    const [summary, missions] = await Promise.all([
      getDashboardSummary(),
      getMissions({ limit: 200 }),
    ]);
    const adaptedMissions = orderMissionsForDisplay(missions.map(toMission));

    return {
      summaryMetrics: toSummaryMetrics(summary),
      attentionQueue: adaptedMissions.filter(
        (mission) => mission.status === "PausedQuota" || mission.status === "Failed",
      ),
      reviewQueue: adaptedMissions.filter(
        (mission) => mission.status === "PullRequestCreated",
      ),
      missions: adaptedMissions,
      usingMockFallback: false,
    };
  } catch {
    const fallbackMissions = orderMissionsForDisplay(mockMissions);

    return {
      summaryMetrics: dashboardSummary,
      attentionQueue: fallbackMissions.filter(
        (mission) => mission.status === "PausedQuota" || mission.status === "Failed",
      ),
      reviewQueue: orderMissionsForDisplay(mockReviewQueue),
      missions: fallbackMissions,
      usingMockFallback: true,
    };
  }
}

export async function getMissionList() {
  try {
    const missions = await getMissions({ limit: 200 });
    return orderMissionsForDisplay(missions.map(toMission));
  } catch {
    return orderMissionsForDisplay(mockMissions);
  }
}

export async function getMissionDetail(id: string) {
  try {
    const mission = await getMission(id);

    if (!mission) {
      return null;
    }

    const [events, summaries] = await Promise.all([
      getMissionEvents(id),
      getMissionSummaries(id),
    ]);

    return {
      mission: toMission(mission),
      events: events.map(toMissionEvent),
      summaries: summaries.map(toMissionSummary),
      logs: buildMissionLogs(toMission(mission), mission.planPath),
      usingMockFallback: false,
    };
  } catch {
    const mission = getMockMissionById(id);
    if (!mission) {
      return null;
    }

    return {
      mission,
      events: getMockMissionEvents(id),
      summaries: [],
      logs: getMockMissionLogs(id),
      usingMockFallback: true,
    };
  }
}

export function toSummaryMetrics(summary: ApiDashboardSummary): SummaryMetric[] {
  return [
    {
      id: "active",
      label: "Active missions",
      value: summary.activeMissions.toString(),
      description: "Currently flowing through planning, coding, or verification.",
      tone: "active",
    },
    {
      id: "paused",
      label: "Paused missions",
      value: summary.pausedMissions.toString(),
      description: "Waiting for quota recovery before execution can continue.",
      tone: "warning",
    },
    {
      id: "failed",
      label: "Failed missions",
      value: summary.failedMissions.toString(),
      description: "Require human inspection before another repair attempt.",
      tone: "danger",
    },
    {
      id: "review",
      label: "PRs awaiting review",
      value: summary.reviewMissions.toString(),
      description: "Work is ready for a human to review and decide next action.",
      tone: "review",
    },
    {
      id: "completed",
      label: "Completed missions",
      value: summary.completedMissions.toString(),
      description: "Closed out after the branch, tests, and pull request lifecycle.",
      tone: "complete",
    },
  ];
}

export function toMission(mission: ApiMission): Mission {
  const status = mapMissionStatus(mission.status);

  return {
    id: mission.id,
    workItemId: mission.externalWorkItemId ?? "Unknown",
    title: mission.title,
    status,
    rawStatus: mission.status,
    statusNote: createStatusNote(status, mission),
    operatorSignal: createOperatorSignal(status, mission),
    branch: mission.branch ?? "No branch recorded",
    pullRequestUrl: mission.pullRequestUrl ?? undefined,
    retryAfter: mission.retryAfter ? formatTimestamp(mission.retryAfter) : undefined,
    lastError: mission.lastError ?? undefined,
    createdAt: formatTimestamp(mission.createdAt),
    updatedAt: formatTimestamp(mission.updatedAt),
  };
}

export function toMissionEvent(event: ApiMissionEvent): MissionEvent {
  const metadataDetail = parseMetadataDetail(event.metadataJson);

  return {
    id: event.id,
    missionId: event.missionId,
    type: event.eventType,
    category: mapEventCategory(event.eventType),
    summary: createEventSummary(event.eventType, event.message),
    detail: metadataDetail
      ? `${event.message} Context: ${metadataDetail}`
      : event.message,
    occurredAt: formatTimestamp(event.createdAt),
    level: mapEventLevel(event.level),
  };
}

export function toMissionSummary(summary: ApiRunSummary): MissionSummary {
  return {
    id: summary.id,
    type: summary.summaryType,
    title: createSummaryTitle(summary.summaryType),
    content: summary.content,
    path: summary.path ?? undefined,
    createdAt: formatTimestamp(summary.createdAt),
  };
}

function orderMissionsForDisplay(missions: Mission[]): Mission[] {
  return [...missions].sort((left, right) => {
    const numberDiff = getMissionSortNumber(right) - getMissionSortNumber(left);
    if (numberDiff !== 0) {
      return numberDiff;
    }

    return left.id.localeCompare(right.id) * -1;
  });
}

function getMissionSortNumber(mission: Mission): number {
  const workItemNumber = Number.parseInt(mission.workItemId, 10);
  if (Number.isFinite(workItemNumber)) {
    return workItemNumber;
  }

  const match = mission.id.match(/(\d+)$/);
  return match ? Number.parseInt(match[1], 10) : Number.NEGATIVE_INFINITY;
}

function mapMissionStatus(rawStatus: string): MissionStatus {
  switch (rawStatus) {
    case "PausedQuota":
      return "PausedQuota";
    case "Failed":
    case "TestsFailed":
    case "Cancelled":
      return "Failed";
    case "PullRequestCreated":
      return "PullRequestCreated";
    case "Completed":
      return "Completed";
    case "Testing":
    case "TestsPassed":
      return "Testing";
    case "Coding":
    case "CodingCompleted":
      return "Coding";
    case "PlanReady":
      return "PlanReady";
    case "Planning":
      return "Planning";
    case "BranchCreated":
    case "New":
    default:
      return "New";
  }
}

function createStatusNote(status: MissionStatus, mission: ApiMission): string {
  switch (status) {
    case "New":
      return "The mission has been registered but no agent has picked it up yet. It is waiting for the daemon or a run-once to start planning.";
    case "Planning":
      return "The planner is currently producing the technical plan. Treat this as an early pipeline state where context and plan quality matter most.";
    case "PlanReady":
      return "The plan has been written. The mission is waiting for the executor stage to start coding.";
    case "Coding":
      return "Implementation is active on the mission branch. Expect code and logs to keep changing until verification starts.";
    case "Testing":
      return mission.lastError
        ? `Verification is unstable. Latest failure: ${mission.lastError}`
        : "Implementation is in verification. The mission is proving the current diff before it can move forward.";
    case "PausedQuota":
      return mission.retryAfter
        ? `The mission paused after a provider quota signal and is scheduled to retry at ${formatTimestamp(mission.retryAfter)}.`
        : "The mission paused after a provider quota signal and is waiting for a safe retry window.";
    case "Failed":
      return mission.lastError
        ? `The mission is no longer self-healing. Latest recorded error: ${mission.lastError}`
        : "The mission stopped in a failed state and likely needs human inspection.";
    case "PullRequestCreated":
      return "Implementation and verification are done. The mission is now waiting for human review on the pull request.";
    case "Completed":
      return "The mission finished its branch, verification, and review handoff lifecycle successfully.";
  }
}

function createOperatorSignal(status: MissionStatus, mission: ApiMission): string {
  switch (status) {
    case "New":
      return "Nothing to act on. Watch only if the mission stays here longer than the poll interval.";
    case "Planning":
      return "Healthy early-stage flow. Watch for plan quality, not repository state.";
    case "PlanReady":
      return "The handoff from planner to executor is the watchpoint. Long stays here usually mean executor wasn't triggered.";
    case "Coding":
      return "Mission is in motion. Intervention is unnecessary unless execution stalls.";
    case "Testing":
      return mission.lastError
        ? "Verification is the fragile point right now. A red test here often determines whether the mission can repair itself."
        : "Verification is active. The key signal is whether tests converge cleanly without a repair loop.";
    case "PausedQuota":
      return "No code defect is implied yet. The correct move is usually to wait unless the retry timing looks suspicious.";
    case "Failed":
      return "High attention. This mission likely needs a human decision before another automated attempt.";
    case "PullRequestCreated":
      return "Agent work is complete. The bottleneck is reviewer attention, not implementation throughput.";
    case "Completed":
      return "No action needed. Keep this state visible for audit confidence and throughput tracking.";
  }
}

function buildMissionLogs(
  mission: Mission,
  planPath: string | null,
): MissionLogLink[] {
  const runRoot =
    planPath?.replace(/\/plan\.md$/u, "") ??
    `${mission.branch}/.agent/runs/issue-${mission.workItemId}`;

  return [
    {
      label: "plan.md",
      path: planPath ?? `${runRoot}/plan.md`,
      kind: "Document",
    },
    {
      label: "claude-plan.log",
      path: `${runRoot}/claude-plan.log`,
      kind: "Log",
    },
    {
      label: "codex-exec.log",
      path: `${runRoot}/codex-exec.log`,
      kind: "Log",
    },
    {
      label: "tests.log",
      path: `${runRoot}/tests.log`,
      kind: "Log",
    },
    {
      label: "repair-attempt-1.log",
      path: `${runRoot}/repair-attempt-1.log`,
      kind: "Log",
    },
  ];
}

function mapEventLevel(level: string): MissionEvent["level"] {
  switch (level.toLowerCase()) {
    case "success":
      return "success";
    case "warning":
      return "warning";
    case "error":
      return "error";
    default:
      return "info";
  }
}

function mapEventCategory(eventType: string): MissionEvent["category"] {
  switch (eventType) {
    case "MissionStarted":
    case "MissionCompleted":
    case "MissionFailed":
    case "MissionResumed":
    case "MissionPausedQuota":
    case "DependencyBlocked":
      return "Lifecycle";
    case "PlanningStarted":
    case "PlanningCompleted":
    case "PlanningFailed":
      return "Planning";
    case "BranchPrepared":
    case "ExecutionStarted":
    case "ExecutionCompleted":
    case "ExecutionFailed":
    case "RepairStarted":
    case "RepairCompleted":
      return "Execution";
    case "VerificationStarted":
    case "VerificationCompleted":
    case "VerificationFailed":
      return "Verification";
    case "SubmitStarted":
    case "SubmitFailed":
    case "PullRequestCreated":
      return "Submit";
    default:
      return "Summary";
  }
}

function createEventSummary(eventType: string, fallback: string): string {
  switch (eventType) {
    case "MissionPausedQuota":
      return "Mission paused after quota or rate-limit signal";
    case "MissionFailed":
      return "Mission stopped and needs operator attention";
    case "MissionCompleted":
      return "Mission reached a completed handoff state";
    case "MissionResumed":
      return "Mission resumed from a paused or failed state";
    case "DependencyBlocked":
      return "Mission could not start because dependencies are unresolved";
    case "SubmitStarted":
      return "Submit phase started";
    case "SubmitFailed":
      return "Submit phase failed";
    default:
      return fallback;
  }
}

function createSummaryTitle(summaryType: string): string {
  switch (summaryType) {
    case "SuccessSummary":
      return "Success summary";
    case "FailureSummary":
      return "Failure summary";
    case "ResumeContext":
      return "Resume context";
    default:
      return summaryType;
  }
}

function parseMetadataDetail(metadataJson: string | null): string | null {
  if (!metadataJson) {
    return null;
  }

  try {
    const value = JSON.parse(metadataJson) as Record<string, unknown>;
    return Object.entries(value)
      .map(([key, entry]) => `${key}=${String(entry)}`)
      .join(", ");
  } catch {
    return metadataJson;
  }
}

function formatTimestamp(value: string): string {
  return value.replace("T", " ").replace("+00:00", " UTC");
}
