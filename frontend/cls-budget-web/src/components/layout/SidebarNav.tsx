"use client";

import { AppLink as Link } from "@/components/AppLink";
import { usePathname } from "next/navigation";
import type { LucideIcon } from "lucide-react";
import {
  ArrowUpFromLine,
  Banknote,
  CreditCard,
  Home,
  LayoutGrid,
  Wallet,
  WalletCards,
} from "lucide-react";
import { SidebarToggle } from "@/components/layout/SidebarToggle";
import { UserMenu } from "@/features/auth/components/UserMenu";

type NavChild = {
  href: string;
  label: string;
};

type NavItem = {
  href: string;
  label: string;
  icon: LucideIcon;
  children?: readonly NavChild[];
};

const navItems: readonly NavItem[] = [
  { href: "/", label: "Overview", icon: Home },
  { href: "/accounts", label: "Accounts", icon: Wallet },
  {
    href: "/credit-cards",
    label: "Credit cards",
    icon: WalletCards,
    children: [
      { href: "/credit-cards/payoff", label: "Payoff Calculator" },
      { href: "/credit-cards/payoff/active", label: "Active Plan" },
    ],
  },
  { href: "/income", label: "Income", icon: Banknote },
  { href: "/payments", label: "Payments", icon: CreditCard },
  { href: "/transactions", label: "Transactions", icon: ArrowUpFromLine },
  { href: "/budgets", label: "Budgets", icon: LayoutGrid },
];

function isActivePath(pathname: string, href: string): boolean {
  return href === "/" ? pathname === "/" : pathname.startsWith(href);
}

export function SidebarNav() {
  const pathname = usePathname();

  return (
    <aside className="hidden w-56 shrink-0 flex-col border-r border-[var(--border)] bg-[var(--card)] px-3 py-6 lg:flex lg:min-h-screen">
      <div className="flex items-center justify-between gap-2 px-1">
        <Link href="/" className="px-2 text-lg font-bold tracking-tight">
          CLS<span className="text-[var(--link)]">Budget</span>
        </Link>
        <SidebarToggle />
      </div>
      <nav className="mt-8 flex flex-col gap-1">
        {navItems.map(({ href, label, icon: Icon, children }) => {
          const active = isActivePath(pathname, href);
          const childActive =
            children?.some((child) => isActivePath(pathname, child.href)) ??
            false;
          const parentActive = active || childActive;

          return (
            <div key={href} className="flex flex-col gap-0.5">
              <Link
                href={href}
                className={`flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition ${
                  parentActive
                    ? "bg-[var(--accent-soft)] text-[var(--link)]"
                    : "text-[var(--muted)] hover:bg-black/[0.04] hover:text-[var(--foreground)]"
                }`}
              >
                <Icon className="h-5 w-5" strokeWidth={parentActive ? 2.5 : 2} />
                {label}
              </Link>
              {children && parentActive
                ? children.map((child) => {
                    const childIsActive = isActivePath(pathname, child.href);
                    return (
                      <Link
                        key={child.href}
                        href={child.href}
                        className={`ml-4 rounded-xl px-3 py-2 text-sm font-medium transition ${
                          childIsActive
                            ? "bg-[var(--accent-soft)] text-[var(--link)]"
                            : "text-[var(--muted)] hover:bg-black/[0.04] hover:text-[var(--foreground)]"
                        }`}
                      >
                        {child.label}
                      </Link>
                    );
                  })
                : null}
            </div>
          );
        })}
      </nav>
      <UserMenu />
    </aside>
  );
}
