import Link from "next/link";

export default function NotFound() {
  return (
    <div className="flex min-h-[60vh] items-center justify-center">
      <div className="max-w-xl rounded-[28px] border border-[var(--panel-border)] bg-[var(--panel)] p-8 text-center shadow-[var(--shadow-card)]">
        <p className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--muted-foreground)]">
          Mission lookup
        </p>
        <h1 className="mt-3 text-3xl font-semibold tracking-tight text-[var(--foreground)]">
          Mission not found
        </h1>
        <p className="mt-4 text-base leading-7 text-[var(--muted-foreground)]">
          The requested mocked mission id does not exist in this first frontend
          shell.
        </p>
        <Link
          href="/missions"
          className="mt-6 inline-flex rounded-full border border-[var(--panel-border-strong)] px-5 py-3 text-sm font-medium text-[var(--foreground)] transition hover:border-[var(--accent)] hover:text-[var(--accent-strong)]"
        >
          Back to missions
        </Link>
      </div>
    </div>
  );
}
