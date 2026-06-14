import { getAccountColor, getAccountInitials } from "@/lib/accountColor";

export function AccountAvatar({
  name,
  size = 44,
  className = "",
}: {
  name: string;
  size?: number;
  className?: string;
}) {
  const bg = getAccountColor(name);
  const initials = getAccountInitials(name);
  const fontSize = Math.max(11, Math.round(size * 0.32));

  return (
    <div
      className={`flex shrink-0 items-center justify-center rounded-full font-semibold text-white ${className}`}
      style={{
        width: size,
        height: size,
        backgroundColor: bg,
        fontSize,
      }}
      aria-hidden
    >
      {initials}
    </div>
  );
}
