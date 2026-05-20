import { Card } from "@/components/ui/Card";
import { SectionTitle } from "@/components/ui/SectionTitle";
import { LayoutGrid } from "lucide-react";

export function BudgetList() {
  return (
    <div className="space-y-6">
      <Card className="flex flex-col items-center p-10 text-center">
        <div className="flex h-14 w-14 items-center justify-center rounded-full bg-[var(--accent-soft)]">
          <LayoutGrid className="h-7 w-7 text-[var(--link)]" />
        </div>
        <p className="mt-4 font-semibold">Budget periods</p>
        <p className="mt-1 max-w-xs text-sm text-[var(--muted)]">
          Organize spending by month and budget template.
        </p>
      </Card>
      <section>
        <SectionTitle title="Coming soon" />
        <Card className="p-4 text-sm text-[var(--muted)]">
          Budget calendar and template management will appear here.
        </Card>
      </section>
    </div>
  );
}
