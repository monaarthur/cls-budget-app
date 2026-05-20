const palette = [
  "#7B61FF",
  "#5B8DEF",
  "#3ECF8E",
  "#F5A623",
  "#FF6B6B",
  "#C56CF0",
  "#48DBFB",
  "#FF9FF3",
] as const;

export function getAccountColor(name: string): string {
  let hash = 0;
  for (let i = 0; i < name.length; i++) {
    hash = name.charCodeAt(i) + ((hash << 5) - hash);
  }
  return palette[Math.abs(hash) % palette.length];
}

export function getAccountInitials(name: string): string {
  const parts = name.trim().split(/\s+/);
  if (parts.length >= 2) {
    return `${parts[0][0]}${parts[1][0]}`.toUpperCase();
  }
  return name.slice(0, 2).toUpperCase();
}
