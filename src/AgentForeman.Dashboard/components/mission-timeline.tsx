import { StatusBadge } from "@/components/status-badge";
import type { Mission, MissionEvent } from "@/lib/types";

const levelStyles: Record<MissionEvent["level"], string> = {
  info: "border-sky-200 bg-sky-100 text-sky-900",
  success: "border-emerald-200 bg-emerald-100 text-emerald-900",
  warning: "border-amber-200 bg-amber-100 text-amber-900",
  error: "border-rose-200 bg-rose-100 text-rose-900",
};

export function MissionTimeline({
  mission,
  events,
}: {
  mission: Mission;
  events: MissionEvent[];
}) {
  return (
    <section className="rounded-[28px] border border-[var(--panel-border)] bg-[var(--panel)] p-6 shadow-[var(--shadow-card)]">
      <div className="flex flex-col gap-4 md:flex-row md:items-end md:justify-between">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--muted-foreground)]">
            Timeline
          </p>
          <h2 className="mt-2 text-2xl font-semibold text-[var(--foreground)]">
            Mission event trail
          </h2>
        </div>
        <div className="flex items-center gap-3">
          <span className="text-sm text-[var(--muted-foreground)]">Current state</span>
          <StatusBadge status={mission.status} context="mission" />
        </div>
      </div>

      <div className="mt-8 space-y-5">
        {events.map((event, index) => (
          <div key={event.id} className="grid gap-4 md:grid-cols-[1rem_minmax(0,1fr)]">
            <div className="relative flex justify-center">
              <span
                className={`mt-2 h-3 w-3 rounded-full border ${levelStyles[event.level]}`}
              />
              {index < events.length - 1 ? (
                <span className="absolute top-6 h-full w-px bg-[var(--panel-border-strong)]" />
              ) : null}
            </div>
            <article className="rounded-[24px] border border-[var(--panel-border)] bg-[var(--surface)] p-5">
              <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
                <div>
                  <p className="text-sm font-semibold uppercase tracking-[0.18em] text-[var(--muted-foreground)]">
                    {event.type}
                  </p>
                  <h3 className="mt-2 text-lg font-semibold text-[var(--foreground)]">
                    {event.summary}
                  </h3>
                </div>
                <span
                  className={`inline-flex rounded-full border px-3 py-1 text-xs font-semibold uppercase tracking-[0.18em] ${levelStyles[event.level]}`}
                >
                  {event.level}
                </span>
              </div>
              <p className="mt-3 text-sm leading-6 text-[var(--muted-foreground)]">
                {event.detail}
              </p>
              <p className="mt-4 font-mono text-xs text-[var(--muted-foreground)]">
                {event.occurredAt}
              </p>
            </article>
          </div>
        ))}
      </div>
    </section>
  );
}
