import Link from "next/link";
import { TopBar } from "@/components/layout/TopBar";
import { CreditCardCalculatorHub } from "@/features/credit-cards/calculators/components/CreditCardCalculatorHub";

export default function CreditCardCalculatorsPage() {
  return (
    <>
      <TopBar
        title="Credit Card Calculator"
        actions={
          <Link
            href="/credit-cards"
            className="text-sm font-medium text-[var(--link)] hover:underline"
          >
            Credit cards
          </Link>
        }
      />
      <CreditCardCalculatorHub />
    </>
  );
}
