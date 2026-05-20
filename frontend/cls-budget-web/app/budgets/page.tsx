import { BudgetList } from "@/features/budgets";
import { TopBar } from "@/components/layout/TopBar";

export default function BudgetsPage() {
  return (
    <>
      <TopBar title="Budgets" />
      <BudgetList />
    </>
  );
}
