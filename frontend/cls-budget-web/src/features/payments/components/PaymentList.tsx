import { Card } from "@/components/ui/Card";
import { SectionTitle } from "@/components/ui/SectionTitle";
import { CreditCard } from "lucide-react";

export function PaymentList() {
  return (
    <div className="space-y-6">
      <Card className="flex flex-col items-center p-10 text-center">
        <div className="flex h-14 w-14 items-center justify-center rounded-full bg-[var(--accent-soft)]">
          <CreditCard className="h-7 w-7 text-[var(--link)]" />
        </div>
        <p className="mt-4 font-semibold">Payments</p>
        <p className="mt-1 max-w-xs text-sm text-[var(--muted)]">
          Track cleared and pending payments across your budget periods.
        </p>
      </Card>
      <section>
        <SectionTitle title="Coming soon" />
        <Card className="p-4 text-sm text-[var(--muted)]">
          Payment history and status filters will appear here.
        </Card>
      </section>
    </div>
  );
}
