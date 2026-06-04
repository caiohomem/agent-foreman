import type { MissionStatus, SystemCheckStatus } from "@/lib/types";

type StatusBadgeProps = {
  status: MissionStatus | SystemCheckStatus;
  context: "mission" | "system";
};

const missionStyles: Record<MissionStatus, string> = {
  New: "border-slate-300 bg-slate-100 text-slate-900",
  Planning: "border-sky-200 bg-sky-100 text-sky-900",
  PlanReady: "border-cyan-200 bg-cyan-100 text-cyan-900",
  Coding: "border-indigo-200 bg-indigo-100 text-indigo-900",
  Testing: "border-violet-200 bg-violet-100 text-violet-900",
  PausedQuota: "border-amber-200 bg-amber-100 text-amber-900",
  Failed: "border-rose-200 bg-rose-100 text-rose-900",
  PullRequestCreated: "border-emerald-200 bg-emerald-100 text-emerald-900",
  Completed: "border-slate-300 bg-slate-200 text-slate-900",
};

const systemStyles: Record<SystemCheckStatus, string> = {
  OK: "border-emerald-200 bg-emerald-100 text-emerald-900",
  Warning: "border-amber-200 bg-amber-100 text-amber-900",
  Failed: "border-rose-200 bg-rose-100 text-rose-900",
};

export function StatusBadge({ status, context }: StatusBadgeProps) {
  const classes =
    context === "mission"
      ? missionStyles[status as MissionStatus]
      : systemStyles[status as SystemCheckStatus];

  return (
    <span
      className={`inline-flex items-center rounded-full border px-3 py-1 text-xs font-semibold tracking-[0.18em] uppercase ${classes}`}
    >
      {status}
    </span>
  );
}
