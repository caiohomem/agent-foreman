import Link from "next/link";
import { SummaryCard } from "@/components/summary-card";
import { StatusBadge } from "@/components/status-badge";
import { systemChecks } from "@/lib/mockData";
import { getDashboardOverview } from "@/lib/dashboard-data";

export const dynamic = "force-dynamic";

export default async function HomePage() {
  const { attentionQueue, reviewQueue, summaryMetrics } =
    await getDashboardOverview();
  const systemWarnings = systemChecks.filter((check) => check.status !== "OK");

  return (
    <div className="space-y-8">
      <section className="rounded-[28px] border border-[var(--panel-border)] bg-[var(--panel)] p-8 shadow-[var(--shadow-card)]">
        <p className="text-xs font-semibold uppercase tracking-[0.28em] text-[var(--muted-foreground)]">
          Agent operations
        </p>
        <div className="mt-4 flex flex-col gap-6 lg:flex-row lg:items-end lg:justify-between">
          <div className="max-w-3xl space-y-3">
            <h1 className="text-4xl font-semibold tracking-tight text-[var(--foreground)]">
              Agent Foreman
            </h1>
            <p className="max-w-2xl text-lg leading-8 text-[var(--muted-foreground)]">
              Mission control for AI coding agents.
            </p>
          </div>
          <div className="grid min-w-[18rem] gap-3 rounded-3xl border border-[var(--panel-border-strong)] bg-[var(--surface-strong)] p-4">
            <p className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--muted-foreground)]">
              Operating picture
            </p>
            <p className="text-sm leading-6 text-[var(--foreground)]">
              {attentionQueue.length} missions currently need operator attention.{" "}
              {reviewQueue.length} pull request
              {reviewQueue.length === 1 ? "" : "s"} waiting for review.
            </p>
          </div>
        </div>
      </section>

      <section className="grid gap-4 md:grid-cols-2 xl:grid-cols-5">
        {summaryMetrics.map((metric) => (
          <SummaryCard key={metric.id} metric={metric} />
        ))}
      </section>

      <section className="grid gap-6 xl:grid-cols-[1.2fr_0.8fr]">
        <div className="rounded-[28px] border border-[var(--panel-border)] bg-[var(--panel)] p-6 shadow-[var(--shadow-card)]">
          <div className="flex items-center justify-between gap-4">
            <div>
              <p className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--muted-foreground)]">
                Needs attention
              </p>
              <h2 className="mt-2 text-xl font-semibold text-[var(--foreground)]">
                Missions that deserve a second look
              </h2>
            </div>
            <Link
              href="/missions"
              className="rounded-full border border-[var(--panel-border-strong)] px-4 py-2 text-sm font-medium text-[var(--foreground)] transition hover:border-[var(--accent)] hover:text-[var(--accent-strong)]"
            >
              Open missions
            </Link>
          </div>

          <div className="mt-6 space-y-4">
            {attentionQueue.map((mission) => (
              <Link
                key={mission.id}
                href={`/missions/${mission.id}`}
                className="block rounded-3xl border border-[var(--panel-border)] bg-[var(--surface)] p-5 transition hover:-translate-y-0.5 hover:border-[var(--panel-border-strong)]"
              >
                <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
                  <div className="space-y-2">
                    <div className="flex items-center gap-3">
                      <code className="rounded-full bg-[var(--surface-strong)] px-3 py-1 text-xs text-[var(--muted-foreground)]">
                        {mission.id}
                      </code>
                      <StatusBadge status={mission.status} context="mission" />
                    </div>
                    <h3 className="text-lg font-semibold text-[var(--foreground)]">
                      {mission.title}
                    </h3>
                    <p className="text-sm leading-6 text-[var(--muted-foreground)]">
                      {mission.statusNote}
                    </p>
                  </div>
                  <dl className="grid gap-3 text-sm lg:min-w-60">
                    <div>
                      <dt className="text-[var(--muted-foreground)]">Work item</dt>
                      <dd className="font-medium text-[var(--foreground)]">
                        {mission.workItemId}
                      </dd>
                    </div>
                    <div>
                      <dt className="text-[var(--muted-foreground)]">Branch</dt>
                      <dd className="font-medium text-[var(--foreground)]">
                        <code>{mission.branch}</code>
                      </dd>
                    </div>
                  </dl>
                </div>
              </Link>
            ))}
          </div>
        </div>

        <div className="space-y-6">
          <div className="rounded-[28px] border border-[var(--panel-border)] bg-[var(--panel)] p-6 shadow-[var(--shadow-card)]">
            <p className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--muted-foreground)]">
              Review queue
            </p>
            <h2 className="mt-2 text-xl font-semibold text-[var(--foreground)]">
              Pull requests awaiting humans
            </h2>
            <div className="mt-5 space-y-4">
              {reviewQueue.map((mission) => (
                <div
                  key={mission.id}
                  className="rounded-3xl border border-[var(--panel-border)] bg-[var(--surface)] p-5"
                >
                  <div className="flex items-center gap-3">
                    <StatusBadge status={mission.status} context="mission" />
                    <code className="text-xs text-[var(--muted-foreground)]">
                      {mission.id}
                    </code>
                  </div>
                  <h3 className="mt-3 text-base font-semibold text-[var(--foreground)]">
                    {mission.title}
                  </h3>
                  <p className="mt-2 text-sm leading-6 text-[var(--muted-foreground)]">
                    {mission.statusNote}
                  </p>
                </div>
              ))}
            </div>
          </div>

          <div className="rounded-[28px] border border-[var(--panel-border)] bg-[var(--panel)] p-6 shadow-[var(--shadow-card)]">
            <p className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--muted-foreground)]">
              System snapshot
            </p>
            <h2 className="mt-2 text-xl font-semibold text-[var(--foreground)]">
              Supporting services
            </h2>
            <div className="mt-5 space-y-3">
              {systemChecks.map((check) => (
                <div
                  key={check.id}
                  className="flex items-start justify-between gap-3 rounded-3xl border border-[var(--panel-border)] bg-[var(--surface)] px-4 py-4"
                >
                  <div>
                    <p className="font-medium text-[var(--foreground)]">{check.name}</p>
                    <p className="mt-1 text-sm text-[var(--muted-foreground)]">
                      {check.summary}
                    </p>
                  </div>
                  <StatusBadge status={check.status} context="system" />
                </div>
              ))}
            </div>
            {systemWarnings.length > 0 ? (
              <p className="mt-4 text-sm leading-6 text-[var(--muted-foreground)]">
                {systemWarnings.length} supporting service
                {systemWarnings.length === 1 ? "" : "s"} currently outside a clean
                `OK` state.
              </p>
            ) : null}
          </div>
        </div>
      </section>
    </div>
  );
}
