import { AppLink as Link } from "@/components/AppLink";
import { Card } from "@/components/ui/Card";

const CALCULATORS = [
  {
    title: "Payoff calculator",
    href: "/credit-cards/payoff",
    description:
      "Avalanche / Snowball plans and compare saved plans",
  },
  {
    title: "Balance transfer analyzer",
    href: "/credit-cards/balance-transfer",
    description: "Compare promotional transfer offers",
  },
  {
    title: "Cash flow analyzer",
    href: "/credit-cards/cash-flow",
    description: "Find safe extra payment room",
  },
  {
    title: "Debt forecast",
    href: "/credit-cards/forecast",
    description: "Month-by-month debt projection",
  },
] as const;

export function CreditCardCalculatorHub() {
  return (
    <div className="space-y-4">
      <p className="text-sm text-[var(--muted)]">
        Choose a calculator to plan payoff, transfers, cash flow, or forecasts.
      </p>
      <ul className="grid gap-3 sm:grid-cols-2">
        {CALCULATORS.map((calculator) => (
          <li key={calculator.href}>
            <Link href={calculator.href} className="block h-full">
              <Card className="h-full p-5 transition hover:bg-black/[0.02]">
                <h2 className="text-base font-semibold text-[var(--foreground)]">
                  {calculator.title}
                </h2>
                <p className="mt-1 text-sm text-[var(--muted)]">
                  {calculator.description}
                </p>
                <span className="mt-3 inline-block text-sm font-medium text-[var(--link)]">
                  Open →
                </span>
              </Card>
            </Link>
          </li>
        ))}
      </ul>
    </div>
  );
}
