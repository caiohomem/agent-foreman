export type MissionStatus =
  | "Planning"
  | "Coding"
  | "Testing"
  | "PausedQuota"
  | "Failed"
  | "PullRequestCreated"
  | "Completed";

export type Mission = {
  id: string;
  workItemId: string;
  title: string;
  status: MissionStatus;
  statusNote: string;
  operatorSignal: string;
  branch: string;
  pullRequestUrl?: string;
  retryAfter?: string;
  lastError?: string;
  createdAt: string;
  updatedAt: string;
};

export type MissionEvent = {
  id: string;
  missionId: string;
  type: string;
  summary: string;
  detail: string;
  occurredAt: string;
  level: "info" | "success" | "warning" | "error";
};

export type SystemCheckStatus = "OK" | "Warning" | "Failed";

export type SystemCheck = {
  id: string;
  name: string;
  kind: string;
  status: SystemCheckStatus;
  summary: string;
  detail: string;
};

export type SummaryMetric = {
  id: string;
  label: string;
  value: string;
  description: string;
  tone: "active" | "warning" | "danger" | "review" | "complete";
};

export type MissionLogLink = {
  label: string;
  path: string;
  kind: "Document" | "Log";
};
