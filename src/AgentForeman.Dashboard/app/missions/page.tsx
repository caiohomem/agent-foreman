import { MissionTable } from "@/components/mission-table";
import { getMissionList } from "@/lib/dashboard-data";

export const dynamic = "force-dynamic";

export default async function MissionsPage() {
  const missions = await getMissionList();

  return (
    <div className="space-y-6">
      <section className="rounded-[28px] border border-[var(--panel-border)] bg-[var(--panel)] p-8 shadow-[var(--shadow-card)]">
        <p className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--muted-foreground)]">
          Mission roster
        </p>
        <h1 className="mt-3 text-3xl font-semibold tracking-tight text-[var(--foreground)]">
          Missions
        </h1>
        <p className="mt-3 max-w-3xl text-base leading-7 text-[var(--muted-foreground)]">
          Read-only view of the current mission portfolio. Status badges and notes are
          tuned to explain where a mission sits in the delivery pipeline and whether
          it likely needs attention, review, or simple observation.
        </p>
      </section>

      <MissionTable missions={missions} />
    </div>
  );
}
