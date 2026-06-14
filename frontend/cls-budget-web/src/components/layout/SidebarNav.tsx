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
import { UserMenu } from "@/features/auth/components/UserMenu";

const navItems = [
  { href: "/", label: "Overview", icon: Home },
  { href: "/accounts", label: "Accounts", icon: Wallet },
  { href: "/credit-cards", label: "Credit cards", icon: WalletCards },
  { href: "/income", label: "Income", icon: Banknote },
  { href: "/payments", label: "Payments", icon: CreditCard },
  { href: "/budgets", label: "Budgets", icon: LayoutGrid },
] as const;

export function SidebarNav() {
  const pathname = usePathname();

  return (
    <aside className="hidden w-56 shrink-0 flex-col border-r border-[var(--border)] bg-[var(--card)] px-3 py-6 lg:flex lg:min-h-screen">
      <Link href="/" className="px-3 text-lg font-bold tracking-tight">
        CLS<span className="text-[var(--link)]">Budget</span>
      </Link>
      <nav className="mt-8 flex flex-col gap-1">
        {navItems.map(({ href, label, icon: Icon }) => {
          const active =
            href === "/" ? pathname === "/" : pathname.startsWith(href);
          return (
            <Link
              key={href}
              href={href}
              className={`flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition ${
                active
                  ? "bg-[var(--accent-soft)] text-[var(--link)]"
                  : "text-[var(--muted)] hover:bg-black/[0.04] hover:text-[var(--foreground)]"
              }`}
            >
              <Icon className="h-5 w-5" strokeWidth={active ? 2.5 : 2} />
              {label}
            </Link>
          );
        })}
      </nav>
      <UserMenu />
    </aside>
  );
}
