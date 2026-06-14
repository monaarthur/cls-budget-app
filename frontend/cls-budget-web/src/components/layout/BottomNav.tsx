"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  Banknote,
  CreditCard,
  Home,
  LayoutGrid,
  Wallet,
  WalletCards,
} from "lucide-react";

const navItems = [
  { href: "/", label: "Overview", icon: Home },
  { href: "/accounts", label: "Accounts", icon: Wallet },
  { href: "/credit-cards", label: "Cards", icon: WalletCards },
  { href: "/income", label: "Income", icon: Banknote },
  { href: "/payments", label: "Payments", icon: CreditCard },
  { href: "/budgets", label: "Budgets", icon: LayoutGrid },
] as const;

export function BottomNav() {
  const pathname = usePathname();

  return (
    <nav className="fixed inset-x-0 bottom-0 z-50 border-t border-[var(--border)] bg-[var(--card)]/95 backdrop-blur-lg lg:hidden">
      <div className="mx-auto flex max-w-2xl items-stretch justify-around px-2 pb-[env(safe-area-inset-bottom)]">
        {navItems.map(({ href, label, icon: Icon }) => {
          const active =
            href === "/" ? pathname === "/" : pathname.startsWith(href);
          return (
            <Link
              key={href}
              href={href}
              className={`flex flex-1 flex-col items-center gap-0.5 py-2.5 text-[10px] font-medium transition ${
                active
                  ? "text-[var(--link)]"
                  : "text-[var(--muted)] hover:text-[var(--foreground)]"
              }`}
            >
              <Icon
                className="h-5 w-5"
                strokeWidth={active ? 2.5 : 2}
                aria-hidden
              />
              <span>{label}</span>
            </Link>
          );
        })}
      </div>
    </nav>
  );
}
