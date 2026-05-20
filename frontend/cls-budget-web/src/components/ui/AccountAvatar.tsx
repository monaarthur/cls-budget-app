import { getAccountColor, getAccountInitials } from "@/lib/accountColor";

export function AccountAvatar({ name }: { name: string }) {
  const bg = getAccountColor(name);
  const initials = getAccountInitials(name);

  return (
    <div
      className="flex h-11 w-11 shrink-0 items-center justify-center rounded-full text-sm font-semibold text-white"
      style={{ backgroundColor: bg }}
      aria-hidden
    >
      {initials}
    </div>
  );
}
