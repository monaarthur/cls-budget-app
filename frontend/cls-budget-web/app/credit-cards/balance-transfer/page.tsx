import { AppLink as Link } from "@/components/AppLink";
import { TopBar } from "@/components/layout/TopBar";
import { BalanceTransferAnalyzer } from "@/features/credit-cards/balance-transfer/components/BalanceTransferAnalyzer";

export default function BalanceTransferPage() {
  return (
    <>
      <TopBar
        title="Balance transfer"
        actions={
          <Link
            href="/credit-cards/calculators"
            className="text-sm font-medium text-[var(--link)] hover:underline"
          >
            Credit Card Calculator
          </Link>
        }
      />
      <BalanceTransferAnalyzer />
    </>
  );
}
