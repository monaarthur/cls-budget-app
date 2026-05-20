"use client";

import { usePathname } from "next/navigation";

const wideRoutes = ["/accounts/grid"];

export function MainContent({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const wide = wideRoutes.some((route) => pathname.startsWith(route));

  return (
    <main
      className={`mx-auto w-full flex-1 px-4 pb-28 pt-6 lg:pb-8 lg:pt-8 ${
        wide ? "max-w-[1600px]" : "max-w-2xl lg:max-w-3xl"
      }`}
    >
      {children}
    </main>
  );
}
