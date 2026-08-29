import { Suspense } from "react";
import { AutoPayoffPage } from "@/features/auto-payoff/components/AutoPayoffPage";

export default function AutoPayoffRoute() {
  return (
    <Suspense
      fallback={
        <p className="text-sm text-[var(--muted)]">Loading Auto Budget…</p>
      }
    >
      <AutoPayoffPage />
    </Suspense>
  );
}
