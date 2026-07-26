import Link from "next/link";
import { TopBar } from "@/components/layout/TopBar";
import { ForecastPlanner } from "@/features/credit-cards/forecast/components/ForecastPlanner";

export default function ForecastPage() {
  return (
    <>
      <TopBar
        title="Debt forecast"
        actions={
          <Link
            href="/credit-cards/calculators"
            className="text-sm font-medium text-[var(--link)] hover:underline"
          >
            Credit Card Calculator
          </Link>
        }
      />
      <ForecastPlanner />
    </>
  );
}
