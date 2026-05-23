import type { MissionLogLink } from "@/lib/types";

export function LogLinks({ logs }: { logs: MissionLogLink[] }) {
  return (
    <section className="rounded-[28px] border border-[var(--panel-border)] bg-[var(--panel)] p-6 shadow-[var(--shadow-card)]">
      <p className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--muted-foreground)]">
        Generated artifacts
      </p>
      <h2 className="mt-2 text-2xl font-semibold text-[var(--foreground)]">
        Logs and documents
      </h2>
      <div className="mt-6 space-y-3">
        {logs.map((log) => (
          <a
            key={log.path}
            href="#"
            className="block rounded-[22px] border border-[var(--panel-border)] bg-[var(--surface)] p-4 transition hover:border-[var(--panel-border-strong)]"
          >
            <div className="flex items-center justify-between gap-3">
              <div>
                <p className="text-sm font-semibold text-[var(--foreground)]">{log.label}</p>
                <p className="mt-2 font-mono text-xs text-[var(--muted-foreground)]">
                  {log.path}
                </p>
              </div>
              <span className="rounded-full bg-[var(--surface-strong)] px-3 py-1 text-xs font-semibold uppercase tracking-[0.18em] text-[var(--muted-foreground)]">
                {log.kind}
              </span>
            </div>
          </a>
        ))}
      </div>
    </section>
  );
}
