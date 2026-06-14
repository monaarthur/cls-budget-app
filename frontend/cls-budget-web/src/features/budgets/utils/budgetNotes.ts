export function parseBudgetNotes(raw: string | null | undefined): string[] {
  if (raw == null || raw.trim() === "") return [];

  try {
    const parsed: unknown = JSON.parse(raw);
    if (Array.isArray(parsed)) {
      return parsed
        .map((item) => String(item).trim())
        .filter((item) => item.length > 0);
    }
  } catch {
    return [raw.trim()];
  }

  return [];
}

export function serializeBudgetNotes(notes: readonly string[]): string | null {
  const cleaned = notes.map((note) => note.trim()).filter(Boolean);
  if (cleaned.length === 0) return null;
  return JSON.stringify(cleaned);
}
