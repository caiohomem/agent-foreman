import { notFound } from "next/navigation";
import type { ReactNode } from "react";
import { LogLinks } from "@/components/log-links";
import { MissionTimeline } from "@/components/mission-timeline";
import { MissionSummaries } from "@/components/mission-summaries";
import { StatusBadge } from "@/components/status-badge";
import { getMissionDetail } from "@/lib/dashboard-data";

type MissionDetailPageProps = {
  params: Promise<{
    id: string;
  }>;
};

export const dynamic = "force-dynamic";

export default async function MissionDetailPage({
  params,
}: MissionDetailPageProps) {
  const { id } = await params;
  const detail = await getMissionDetail(id);

  if (!detail) {
    notFound();
  }

  const { mission, events, logs, summaries } = detail;

  return (
    <div className="space-y-6">
      <section className="rounded-[28px] border border-[var(--panel-border)] bg-[var(--panel)] p-8 shadow-[var(--shadow-card)]">
        <div className="flex flex-col gap-6 lg:flex-row lg:items-start lg:justify-between">
          <div className="space-y-4">
            <div className="flex flex-wrap items-center gap-3">
              <StatusBadge status={mission.status} context="mission" />
              <code className="rounded-full bg-[var(--surface-strong)] px-3 py-1 text-xs text-[var(--muted-foreground)]">
                {mission.id}
              </code>
            </div>
            <div>
              <h1 className="text-3xl font-semibold tracking-tight text-[var(--foreground)]">
                {mission.title}
              </h1>
              <p className="mt-3 max-w-3xl text-base leading-7 text-[var(--muted-foreground)]">
                {mission.statusNote}
              </p>
            </div>
          </div>

          <div className="w-full max-w-md rounded-[24px] border border-[var(--panel-border-strong)] bg-[var(--surface-strong)] p-5">
            <p className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--muted-foreground)]">
              Operator signal
            </p>
            <p className="mt-3 text-base leading-7 text-[var(--foreground)]">
              {mission.operatorSignal}
            </p>
          </div>
        </div>
      </section>

      <section className="grid gap-6 xl:grid-cols-[1.05fr_0.95fr]">
        <div className="rounded-[28px] border border-[var(--panel-border)] bg-[var(--panel)] p-6 shadow-[var(--shadow-card)]">
          <p className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--muted-foreground)]">
            Mission metadata
          </p>
          <div className="mt-6 grid gap-4 md:grid-cols-2">
            <MetaField label="Mission id" value={<code>{mission.id}</code>} />
            <MetaField label="Work item id" value={<code>{mission.workItemId}</code>} />
            <MetaField label="Title" value={mission.title} />
            <MetaField
              label="Status"
              value={<StatusBadge status={mission.status} context="mission" />}
            />
            <MetaField
              label="Stored status"
              value={<code>{mission.rawStatus ?? mission.status}</code>}
            />
            <MetaField label="Branch" value={<code>{mission.branch}</code>} />
            <MetaField
              label="Pull request URL"
              value={
                mission.pullRequestUrl ? (
                  <a
                    href={mission.pullRequestUrl}
                    className="text-[var(--accent-strong)] underline decoration-[var(--panel-border-strong)] underline-offset-4"
                  >
                    {mission.pullRequestUrl}
                  </a>
                ) : (
                  "Not created yet"
                )
              }
            />
            <MetaField label="Retry after" value={mission.retryAfter ?? "Not scheduled"} />
            <MetaField label="Last error" value={mission.lastError ?? "No error recorded"} />
            <MetaField label="Created at" value={mission.createdAt} />
            <MetaField label="Updated at" value={mission.updatedAt} />
          </div>
        </div>

        <LogLinks logs={logs} />
      </section>

      <MissionSummaries summaries={summaries} />
      <MissionTimeline mission={mission} events={events} />
    </div>
  );
}

function MetaField({
  label,
  value,
}: {
  label: string;
  value: ReactNode;
}) {
  return (
    <div className="rounded-[24px] border border-[var(--panel-border)] bg-[var(--surface)] p-4">
      <p className="text-xs font-semibold uppercase tracking-[0.2em] text-[var(--muted-foreground)]">
        {label}
      </p>
      <div className="mt-3 text-sm leading-6 text-[var(--foreground)]">{value}</div>
    </div>
  );
}
