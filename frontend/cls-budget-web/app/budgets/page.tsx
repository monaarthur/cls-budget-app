import { BudgetsView } from "@/features/budgets/components/BudgetsView";
import { TopBar } from "@/components/layout/TopBar";

export default function BudgetsPage() {
  return (
    <>
      <TopBar title="Budgets" />
      <BudgetsView />
    </>
  );
}
