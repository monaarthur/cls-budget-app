import Link from "next/link";
import { TopBar } from "@/components/layout/TopBar";
import { CashFlowAnalyzer } from "@/features/credit-cards/cash-flow/components/CashFlowAnalyzer";

export default function CashFlowPage() {
  return (
    <>
      <TopBar
        title="Cash flow"
        actions={
          <Link
            href="/credit-cards/calculators"
            className="text-sm font-medium text-[var(--link)] hover:underline"
          >
            Credit Card Calculator
          </Link>
        }
      />
      <CashFlowAnalyzer />
    </>
  );
}
