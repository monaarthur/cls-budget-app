import { CreditCardGrid } from "@/features/accounts/components/CreditCardGrid";
import { TopBar } from "@/components/layout/TopBar";
import Link from "next/link";

export default function CreditCardsGridPage() {
  return (
    <>
      <TopBar title="Credit card grid" />
      <div className="mb-4 flex items-center gap-4 text-sm">
        <Link
          href="/credit-cards"
          className="text-[var(--link)] hover:underline"
        >
          ← Card view
        </Link>
      </div>
      <CreditCardGrid />
    </>
  );
}
