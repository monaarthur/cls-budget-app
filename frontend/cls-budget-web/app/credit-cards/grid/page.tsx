import { CreditCardGrid } from "@/features/accounts/components/CreditCardGrid";
import { TopBar } from "@/components/layout/TopBar";
import { AppLink as Link } from "@/components/AppLink";

export default function CreditCardsGridPage() {
  return (
    <>
      <TopBar title="Credit card grid" />
      <div className="mb-4 flex flex-wrap items-center gap-4 text-sm">
        <Link
          href="/credit-cards"
          className="text-[var(--link)] hover:underline"
        >
          ← Card view
        </Link>
        <Link
          href="/credit-cards/payoff"
          className="text-[var(--link)] hover:underline"
        >
          Payoff calculator →
        </Link>
      </div>
      <CreditCardGrid />
    </>
  );
}
