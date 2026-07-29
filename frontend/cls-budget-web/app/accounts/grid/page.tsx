import { AccountGrid } from "@/features/accounts/components/AccountGrid";
import { TopBar } from "@/components/layout/TopBar";
import { AppLink as Link } from "@/components/AppLink";

export default function AccountsGridPage() {
  return (
    <>
      <TopBar title="Account grid" />
      <div className="mb-4 flex items-center gap-4 text-sm">
        <Link href="/accounts" className="text-[var(--link)] hover:underline">
          ← Card view
        </Link>
      </div>
      <AccountGrid />
    </>
  );
}
