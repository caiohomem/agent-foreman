export type ApiDashboardSummary = {
  totalMissions: number;
  activeMissions: number;
  pausedMissions: number;
  failedMissions: number;
  reviewMissions: number;
  completedMissions: number;
};

export type ApiMission = {
  id: string;
  externalWorkItemId: string | null;
  source: string | null;
  title: string;
  status: string;
  branch: string | null;
  planPath: string | null;
  pullRequestUrl: string | null;
  retryAfter: string | null;
  lastError: string | null;
  createdAt: string;
  updatedAt: string;
  blockedCommentPostedAt: string | null;
};

export type ApiMissionEvent = {
  id: string;
  missionId: string;
  externalWorkItemId: string | null;
  runId: string | null;
  eventType: string;
  level: string;
  message: string;
  metadataJson: string | null;
  createdAt: string;
};

export type ApiRunSummary = {
  id: string;
  missionId: string;
  externalWorkItemId: string | null;
  summaryType: string;
  content: string;
  path: string | null;
  createdAt: string;
};

type MissionQuery = {
  status?: string;
  limit?: number;
};

const apiBaseUrl =
  process.env.AGENT_FOREMAN_API_BASE_URL ?? "http://localhost:52888";

export async function getDashboardSummary(): Promise<ApiDashboardSummary> {
  return fetchJson<ApiDashboardSummary>("/api/dashboard/summary");
}

export async function getMissions(query: MissionQuery = {}): Promise<ApiMission[]> {
  const searchParams = new URLSearchParams();

  if (query.status) {
    searchParams.set("status", query.status);
  }

  if (query.limit) {
    searchParams.set("limit", query.limit.toString());
  }

  const suffix = searchParams.size > 0 ? `?${searchParams.toString()}` : "";
  return fetchJson<ApiMission[]>(`/api/missions${suffix}`);
}

export async function getMission(id: string): Promise<ApiMission | null> {
  return fetchJson<ApiMission>(`/api/missions/${id}`, { allowNotFound: true });
}

export async function getMissionEvents(id: string): Promise<ApiMissionEvent[]> {
  return fetchJson<ApiMissionEvent[]>(`/api/missions/${id}/events`);
}

export async function getMissionSummaries(id: string): Promise<ApiRunSummary[]> {
  return fetchJson<ApiRunSummary[]>(`/api/missions/${id}/summaries`);
}

async function fetchJson<T>(
  path: string,
  options: { allowNotFound?: boolean } = {},
): Promise<T> {
  const response = await fetch(new URL(path, apiBaseUrl), {
    cache: "no-store",
    signal: AbortSignal.timeout(1500),
    headers: {
      Accept: "application/json",
    },
  });

  if (options.allowNotFound && response.status === 404) {
    return null as T;
  }

  if (!response.ok) {
    throw new Error(`API request failed: ${response.status} ${response.statusText}`);
  }

  return (await response.json()) as T;
}
