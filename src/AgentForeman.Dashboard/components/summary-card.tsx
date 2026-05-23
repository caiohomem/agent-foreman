import type { SummaryMetric } from "@/lib/types";

const toneStyles: Record<SummaryMetric["tone"], string> = {
  active: "from-sky-500/12 to-sky-500/0 text-sky-900",
  warning: "from-amber-500/16 to-amber-500/0 text-amber-900",
  danger: "from-rose-500/16 to-rose-500/0 text-rose-900",
  review: "from-emerald-500/14 to-emerald-500/0 text-emerald-900",
  complete: "from-slate-500/16 to-slate-500/0 text-slate-900",
};

export function SummaryCard({ metric }: { metric: SummaryMetric }) {
  return (
    <article className="rounded-[28px] border border-[var(--panel-border)] bg-[var(--panel)] p-5 shadow-[var(--shadow-card)]">
      <div
        className={`rounded-[22px] bg-gradient-to-br p-5 ${toneStyles[metric.tone]}`}
      >
        <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--muted-foreground)]">
          {metric.label}
        </p>
        <p className="mt-4 text-4xl font-semibold tracking-tight">{metric.value}</p>
        <p className="mt-3 text-sm leading-6 text-[var(--foreground)]">
          {metric.description}
        </p>
      </div>
    </article>
  );
}
