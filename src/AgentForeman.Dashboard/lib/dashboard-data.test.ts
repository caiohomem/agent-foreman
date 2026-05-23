import test from "node:test";
import assert from "node:assert/strict";

import {
  toMission,
  toMissionEvent,
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
