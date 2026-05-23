import { StatusBadge } from "@/components/status-badge";
import { systemChecks } from "@/lib/mockData";

export default function SystemPage() {
  return (
    <div className="space-y-6">
      <section className="rounded-[28px] border border-[var(--panel-border)] bg-[var(--panel)] p-8 shadow-[var(--shadow-card)]">
        <p className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--muted-foreground)]">
          Read-only infrastructure view
        </p>
        <h1 className="mt-3 text-3xl font-semibold tracking-tight text-[var(--foreground)]">
          System
        </h1>
        <p className="mt-3 max-w-3xl text-base leading-7 text-[var(--muted-foreground)]">
          Mocked service health view for the initial dashboard shell. This is where
          database access, tool availability, configuration, and daemon status can be
          monitored once the real backend integration is added.
        </p>
      </section>

      <section className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
        {systemChecks.map((check) => (
          <article
            key={check.id}
            className="rounded-[28px] border border-[var(--panel-border)] bg-[var(--panel)] p-6 shadow-[var(--shadow-card)]"
          >
            <div className="flex items-start justify-between gap-4">
              <div>
                <p className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--muted-foreground)]">
                  {check.kind}
                </p>
                <h2 className="mt-2 text-xl font-semibold text-[var(--foreground)]">
                  {check.name}
                </h2>
              </div>
              <StatusBadge status={check.status} context="system" />
            </div>
            <p className="mt-4 text-sm font-medium text-[var(--foreground)]">
              {check.summary}
            </p>
            <p className="mt-3 text-sm leading-6 text-[var(--muted-foreground)]">
              {check.detail}
            </p>
          </article>
        ))}
      </section>
    </div>
  );
}
