import Link from "next/link";
import { StatusBadge } from "@/components/status-badge";
import type { Mission } from "@/lib/types";

export function MissionTable({ missions }: { missions: Mission[] }) {
  return (
    <section className="overflow-hidden rounded-[28px] border border-[var(--panel-border)] bg-[var(--panel)] shadow-[var(--shadow-card)]">
      <div className="overflow-x-auto">
        <table className="min-w-full border-separate border-spacing-0">
          <thead>
            <tr className="bg-[var(--surface-strong)]">
              {["Mission", "Work item", "Title", "Status", "Branch", "PR", "Updated"].map(
                (heading) => (
                  <th
                    key={heading}
                    className="border-b border-[var(--panel-border)] px-5 py-4 text-left text-xs font-semibold uppercase tracking-[0.24em] text-[var(--muted-foreground)]"
                  >
                    {heading}
                  </th>
                ),
              )}
            </tr>
          </thead>
          <tbody>
            {missions.map((mission) => (
              <tr key={mission.id} className="align-top">
                <td className="border-b border-[var(--panel-border)] px-5 py-5">
                  <Link
                    href={`/missions/${mission.id}`}
                    className="font-mono text-sm font-medium text-[var(--accent-strong)]"
                  >
                    {mission.id}
                  </Link>
                </td>
                <td className="border-b border-[var(--panel-border)] px-5 py-5">
                  <code className="text-sm text-[var(--foreground)]">{mission.workItemId}</code>
                </td>
                <td className="border-b border-[var(--panel-border)] px-5 py-5">
                  <div className="min-w-72">
                    <p className="font-semibold text-[var(--foreground)]">{mission.title}</p>
                    <p className="mt-2 text-sm leading-6 text-[var(--muted-foreground)]">
                      {mission.statusNote}
                    </p>
                  </div>
                </td>
                <td className="border-b border-[var(--panel-border)] px-5 py-5">
                  <div className="space-y-3">
                    <StatusBadge status={mission.status} context="mission" />
                    <p className="max-w-52 text-sm leading-6 text-[var(--muted-foreground)]">
                      {mission.operatorSignal}
                    </p>
                  </div>
                </td>
                <td className="border-b border-[var(--panel-border)] px-5 py-5">
                  <code className="text-sm text-[var(--foreground)]">{mission.branch}</code>
                </td>
                <td className="border-b border-[var(--panel-border)] px-5 py-5">
                  {mission.pullRequestUrl ? (
                    <a
                      href={mission.pullRequestUrl}
                      className="text-sm font-medium text-[var(--accent-strong)] underline decoration-[var(--panel-border-strong)] underline-offset-4"
                    >
                      Awaiting review
                    </a>
                  ) : (
                    <span className="text-sm text-[var(--muted-foreground)]">Not opened</span>
                  )}
                </td>
                <td className="border-b border-[var(--panel-border)] px-5 py-5 text-sm text-[var(--muted-foreground)]">
                  {mission.updatedAt}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}
