"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

const navigation = [
  { href: "/", label: "Dashboard", shortLabel: "Overview" },
  { href: "/missions", label: "Missions", shortLabel: "Pipeline" },
  { href: "/system", label: "System", shortLabel: "Health" },
];

export function Sidebar() {
  const pathname = usePathname();

  return (
    <aside className="border-b border-white/8 bg-[var(--sidebar)] text-[var(--sidebar-foreground)] lg:min-h-screen lg:border-b-0 lg:border-r lg:border-r-white/8">
      <div className="flex h-full flex-col px-4 py-5 sm:px-6 lg:px-5 lg:py-6">
        <div className="rounded-[28px] border border-white/8 bg-white/4 p-5">
          <p className="text-xs font-semibold uppercase tracking-[0.28em] text-[var(--sidebar-muted)]">
            Agent Foreman
          </p>
          <h2 className="mt-3 text-2xl font-semibold tracking-tight">
            Dashboard
          </h2>
          <p className="mt-3 text-sm leading-6 text-[var(--sidebar-muted)]">
            Monitor mission flow, paused quota states, review queues, and system
            health from one read-only shell.
          </p>
        </div>

        <nav className="mt-6 grid gap-2">
          {navigation.map((item) => {
            const active =
              item.href === "/"
                ? pathname === "/"
                : pathname === item.href || pathname.startsWith(`${item.href}/`);

            return (
              <Link
                key={item.href}
                href={item.href}
                className={`rounded-[22px] border px-4 py-4 transition ${
                  active
                    ? "border-white/14 bg-white/10 text-white shadow-lg"
                    : "border-white/6 bg-white/[0.03] text-[var(--sidebar-foreground)] hover:border-white/10 hover:bg-white/[0.06]"
                }`}
              >
                <div className="flex items-center justify-between gap-3">
                  <span className="text-sm font-semibold">{item.label}</span>
                  <span className="text-xs uppercase tracking-[0.2em] text-[var(--sidebar-muted)]">
                    {item.shortLabel}
                  </span>
                </div>
              </Link>
            );
          })}
        </nav>

        <div className="mt-6 rounded-[24px] border border-white/8 bg-white/[0.03] p-4 text-sm leading-6 text-[var(--sidebar-muted)]">
          Read-only first cut.
          <br />
          No live updates. No write actions.
        </div>
      </div>
    </aside>
  );
}
