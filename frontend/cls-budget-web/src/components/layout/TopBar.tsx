import { getGreeting } from "@/lib/format";

export function TopBar({ title }: { title?: string }) {
  return (
    <header className="mb-6 flex items-center justify-between">
      <div>
        <p className="text-sm text-[var(--muted)]">{getGreeting()}</p>
        <h1 className="text-2xl font-bold tracking-tight">
          {title ?? "Overview"}
        </h1>
      </div>
      <div
        className="flex h-10 w-10 items-center justify-center rounded-full bg-[var(--card-elevated)] text-sm font-semibold text-[var(--muted)]"
        aria-label="Profile"
      >
        U
      </div>
    </header>
  );
}
