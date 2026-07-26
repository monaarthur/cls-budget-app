import Link from "next/link";
import { TopBar } from "@/components/layout/TopBar";
import { CreditCardPayoffCalculator } from "@/features/credit-cards/payoff/components/CreditCardPayoffCalculator";

export default function CreditCardPayoffPage() {
  return (
    <>
      <TopBar
        title="Credit Card Payoff"
        actions={
          <>
            <Link
              href="/credit-cards/calculators"
              className="text-sm font-medium text-[var(--link)] hover:underline"
            >
              Credit Card Calculator
            </Link>
            <Link
              href="/credit-cards"
              className="text-sm font-medium text-[var(--link)] hover:underline"
            >
              Credit Card Home Page
            </Link>
            <Link
              href="/credit-cards/grid"
              className="text-sm font-medium text-[var(--link)] hover:underline"
            >
              Credit Card Details
            </Link>
          </>
        }
      />
      <CreditCardPayoffCalculator />
    </>
  );
}
