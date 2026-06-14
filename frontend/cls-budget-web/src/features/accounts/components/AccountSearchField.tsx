"use client";

import { Search } from "lucide-react";

type AccountSearchFieldProps = {
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  className?: string;
};

export function AccountSearchField({
  value,
  onChange,
  placeholder = "Search accounts…",
  className = "",
}: AccountSearchFieldProps) {
  return (
    <div className={`relative ${className}`.trim()}>
      <Search
        size={16}
        aria-hidden
        className="pointer-events-none absolute left-3.5 top-1/2 -translate-y-1/2 text-[var(--muted)]"
      />
      <input
        type="search"
        value={value}
        onChange={(event) => onChange(event.target.value)}
        placeholder={placeholder}
        aria-label={placeholder}
        className="w-full rounded-full border border-black/10 bg-white px-4 py-2.5 pl-10 text-sm text-[var(--foreground)] outline-none transition placeholder:text-[var(--muted)] focus:border-[var(--accent)] focus:ring-2 focus:ring-[var(--accent)]/20"
      />
    </div>
  );
}
