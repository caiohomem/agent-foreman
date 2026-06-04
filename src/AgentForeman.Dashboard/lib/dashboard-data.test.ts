import test from "node:test";
import assert from "node:assert/strict";

import {
  toMission,
  toMissionEvent,
  toMissionSummary,
  toSummaryMetrics,
} from "./dashboard-data.ts";

test("toSummaryMetrics maps API counts into dashboard cards", () => {
  const metrics = toSummaryMetrics({
    totalMissions: 9,
    activeMissions: 3,
    pausedMissions: 1,
    failedMissions: 2,
    reviewMissions: 1,
    completedMissions: 2,
  });

  assert.equal(metrics[0]?.value, "3");
  assert.equal(metrics[1]?.value, "1");
  assert.equal(metrics[2]?.value, "2");
  assert.equal(metrics[3]?.value, "1");
  assert.equal(metrics[4]?.value, "2");
});

test("toMission adapts API mission details into the richer UI model", () => {
  const mission = toMission({
    id: "github-42",
    externalWorkItemId: "42",
    source: "GitHub",
    title: "Build the dashboard shell",
    status: "PullRequestCreated",
    branch: "agent/issue-42",
    planPath: "/workspace/.agent/runs/issue-42/plan.md",
    pullRequestUrl: "https://github.com/caioh/agent-foreman/pull/42",
    retryAfter: null,
    lastError: null,
    createdAt: "2026-05-23T10:00:00+00:00",
    updatedAt: "2026-05-23T10:45:00+00:00",
    blockedCommentPostedAt: null,
  });

  assert.equal(mission.workItemId, "42");
  assert.equal(mission.status, "PullRequestCreated");
  assert.match(mission.statusNote, /waiting for human review/i);
  assert.match(mission.operatorSignal, /reviewer attention/i);
  assert.equal(mission.pullRequestUrl, "https://github.com/caioh/agent-foreman/pull/42");
});

test("toMission keeps the New status distinct from Planning", () => {
  const mission = toMission({
    id: "github-30",
    externalWorkItemId: "30",
    source: "GitHub",
    title: "Implement proof-of-play reporting",
    status: "New",
    branch: null,
    planPath: null,
    pullRequestUrl: null,
    retryAfter: null,
    lastError: null,
    createdAt: "2026-06-01T10:00:00+00:00",
    updatedAt: "2026-06-01T10:00:00+00:00",
    blockedCommentPostedAt: null,
  });

  assert.equal(mission.status, "New");
  assert.equal(mission.rawStatus, "New");
  assert.match(mission.statusNote, /no agent has picked it up/i);
});

test("toMission keeps PlanReady distinct from Planning", () => {
  const mission = toMission({
    id: "github-30",
    externalWorkItemId: "30",
    source: "GitHub",
    title: "Implement proof-of-play reporting",
    status: "PlanReady",
    branch: null,
    planPath: "/workspace/.agent/runs/issue-30/plan.md",
    pullRequestUrl: null,
    retryAfter: null,
    lastError: null,
    createdAt: "2026-06-01T10:00:00+00:00",
    updatedAt: "2026-06-01T10:05:00+00:00",
    blockedCommentPostedAt: null,
  });

  assert.equal(mission.status, "PlanReady");
  assert.match(mission.statusNote, /plan has been written/i);
});

test("toMissionEvent adapts API events into timeline entries", () => {
  const event = toMissionEvent({
    id: "evt-1",
    missionId: "github-42",
    externalWorkItemId: "42",
    runId: "run-1",
    eventType: "VerificationFailed",
    level: "Error",
    message: "Tests failed: expected 200 got 500",
    metadataJson: "{\"stage\":\"tests\"}",
    createdAt: "2026-05-23T10:50:00+00:00",
  });

  assert.equal(event.type, "VerificationFailed");
  assert.equal(event.level, "error");
  assert.match(event.summary, /Tests failed/i);
  assert.match(event.detail, /stage/i);
});

test("toMissionSummary adapts saved run summaries", () => {
  const summary = toMissionSummary({
    id: "sum-1",
    missionId: "github-42",
    externalWorkItemId: "42",
    summaryType: "ResumeContext",
    content: "## Resume\n- Pick up from verify",
    path: ".agent/runs/issue-42/resume-context.md",
    createdAt: "2026-05-23T11:00:00+00:00",
  });

  assert.equal(summary.type, "ResumeContext");
  assert.equal(summary.title, "Resume context");
  assert.match(summary.content, /Pick up from verify/i);
  assert.match(summary.path ?? "", /resume-context\.md/i);
});
