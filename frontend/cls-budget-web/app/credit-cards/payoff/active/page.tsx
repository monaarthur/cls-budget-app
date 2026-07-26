import Link from "next/link";
import { TopBar } from "@/components/layout/TopBar";
import { ActivePayoffPlanPanel } from "@/features/credit-cards/payoff/components/ActivePayoffPlanPanel";

export default function ActivePayoffPlanPage() {
  return (
    <>
      <TopBar
        title="Active payoff plan"
        actions={
          <>
            <Link
              href="/credit-cards/payoff"
              className="text-sm font-medium text-[var(--link)] hover:underline"
            >
              Payoff Calculator
            </Link>
            <Link
              href="/credit-cards"
              className="text-sm font-medium text-[var(--link)] hover:underline"
            >
              Credit Card Home Page
            </Link>
          </>
        }
      />
      <div className="mx-auto w-full max-w-5xl px-4 py-6">
        <ActivePayoffPlanPanel />
      </div>
    </>
  );
}
