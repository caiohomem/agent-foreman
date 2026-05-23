import type { ReactNode } from "react";
import { Sidebar } from "@/components/sidebar";

export function AppShell({ children }: { children: ReactNode }) {
  return (
    <div className="min-h-screen lg:grid lg:grid-cols-[280px_minmax(0,1fr)]">
      <Sidebar />
      <div className="min-w-0">
        <header className="sticky top-0 z-20 border-b border-[var(--panel-border)] bg-[rgba(244,247,251,0.82)] backdrop-blur-xl">
          <div className="mx-auto flex max-w-[1600px] items-center justify-between gap-4 px-4 py-4 sm:px-6 lg:px-8">
            <div>
              <p className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--muted-foreground)]">
                Agent Foreman
              </p>
              <h1 className="mt-1 text-lg font-semibold text-[var(--foreground)]">
                Mission control
              </h1>
            </div>
            <div className="rounded-full border border-[var(--panel-border-strong)] bg-[var(--panel)] px-4 py-2 text-sm font-medium text-[var(--foreground)] shadow-sm">
              Environment: Local
            </div>
          </div>
        </header>

        <main className="mx-auto max-w-[1600px] px-4 py-6 sm:px-6 lg:px-8 lg:py-8">
          {children}
        </main>
      </div>
    </div>
  );
}
