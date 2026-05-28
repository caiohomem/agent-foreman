import type { MissionSummary } from "@/lib/types";

const summaryTone = {
  SuccessSummary: "border-emerald-200 bg-emerald-50",
  FailureSummary: "border-rose-200 bg-rose-50",
  ResumeContext: "border-amber-200 bg-amber-50",
} as const;

export function MissionSummaries({ summaries }: { summaries: MissionSummary[] }) {
  if (summaries.length === 0) {
    return null;
  }

  return (
    <section className="rounded-[28px] border border-[var(--panel-border)] bg-[var(--panel)] p-6 shadow-[var(--shadow-card)]">
      <div className="flex flex-col gap-2">
        <p className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--muted-foreground)]">
          Summaries
        </p>
        <h2 className="text-2xl font-semibold text-[var(--foreground)]">
          Saved mission summaries
        </h2>
      </div>

      <div className="mt-6 space-y-4">
        {summaries.map((summary) => (
          <article
            key={summary.id}
            className={`rounded-[24px] border p-5 ${
              summaryTone[summary.type as keyof typeof summaryTone] ??
              "border-[var(--panel-border)] bg-[var(--surface)]"
            }`}
          >
            <div className="flex flex-col gap-3 md:flex-row md:items-start md:justify-between">
              <div>
                <p className="text-sm font-semibold uppercase tracking-[0.18em] text-[var(--muted-foreground)]">
                  {summary.type}
                </p>
                <h3 className="mt-2 text-lg font-semibold text-[var(--foreground)]">
                  {summary.title}
                </h3>
              </div>
              <div className="text-right">
                <p className="font-mono text-xs text-[var(--muted-foreground)]">
                  {summary.createdAt}
                </p>
                {summary.path ? (
                  <p className="mt-2 font-mono text-xs text-[var(--muted-foreground)]">
                    {summary.path}
                  </p>
                ) : null}
              </div>
            </div>
            <pre className="mt-4 overflow-x-auto whitespace-pre-wrap rounded-[20px] border border-[var(--panel-border)] bg-white/70 p-4 text-sm leading-6 text-[var(--foreground)]">
              {summary.content}
            </pre>
          </article>
        ))}
      </div>
    </section>
  );
}
