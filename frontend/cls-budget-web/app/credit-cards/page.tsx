import { CreditCardList } from "@/features/accounts/components/CreditCardList";
import { TopBar } from "@/components/layout/TopBar";
import Link from "next/link";

export default function CreditCardsPage() {
  return (
    <>
      <TopBar title="Credit cards" />
      <div className="mb-4">
        <Link
          href="/credit-cards/grid"
          className="text-sm font-medium text-[var(--link)] hover:underline"
        >
          Open editable grid →
        </Link>
      </div>
      <CreditCardList />
    </>
  );
}
