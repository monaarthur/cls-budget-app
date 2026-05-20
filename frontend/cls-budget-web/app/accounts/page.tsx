import { AccountList } from "@/features/accounts";
import { TopBar } from "@/components/layout/TopBar";
import Link from "next/link";

export default function AccountsPage() {
  return (
    <>
      <TopBar title="Accounts" />
      <div className="mb-4">
        <Link
          href="/accounts/grid"
          className="text-sm font-medium text-[var(--link)] hover:underline"
        >
          Open editable grid →
        </Link>
      </div>
      <AccountList />
    </>
  );
}
