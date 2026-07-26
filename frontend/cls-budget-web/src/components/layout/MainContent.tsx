"use client";

export function MainContent({ children }: { children: React.ReactNode }) {
  return (
    <main className="w-full min-w-0 flex-1 px-4 pb-28 pt-6 sm:px-6 lg:px-8 lg:pb-8 lg:pt-8">
      {children}
    </main>
  );
}
