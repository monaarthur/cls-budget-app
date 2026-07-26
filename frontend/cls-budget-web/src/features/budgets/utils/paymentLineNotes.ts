export interface PaymentLineNoteItem {
  id: string;
  text: string;
  createdAt: string;
  /** Soft-removed notes stay in storage but are hidden from the UI. */
  removedAt: string | null;
}

function newNoteId(): string {
  if (typeof crypto !== "undefined" && "randomUUID" in crypto) {
    return crypto.randomUUID();
  }
  return `note-${Date.now()}-${Math.random().toString(36).slice(2, 10)}`;
}

export function createPaymentLineNote(text: string, createdAt = new Date()): PaymentLineNoteItem {
  return {
    id: newNoteId(),
    text: text.trim(),
    createdAt: createdAt.toISOString(),
    removedAt: null,
  };
}

export function parsePaymentLineNotes(
  raw: string | null | undefined,
): PaymentLineNoteItem[] {
  if (raw == null || raw.trim() === "") return [];

  try {
    const parsed: unknown = JSON.parse(raw);

    if (Array.isArray(parsed)) {
      if (parsed.every((item) => typeof item === "string")) {
        return parsed
          .map((item) => String(item).trim())
          .filter(Boolean)
          .map((text) => createPaymentLineNote(text));
      }

      return parsed
        .map((item) => normalizeNoteItem(item))
        .filter((item): item is PaymentLineNoteItem => item != null);
    }

    if (parsed && typeof parsed === "object") {
      const item = normalizeNoteItem(parsed);
      return item ? [item] : [];
    }
  } catch {
    const trimmed = raw.trim();
    return trimmed.length > 0 ? [createPaymentLineNote(trimmed)] : [];
  }

  return [];
}

function normalizeNoteItem(value: unknown): PaymentLineNoteItem | null {
  if (!value || typeof value !== "object") return null;
  const record = value as Record<string, unknown>;
  const text = String(record.text ?? "").trim();
  if (!text) return null;

  const createdAtRaw = record.createdAt;
  const createdAt =
    typeof createdAtRaw === "string" && Number.isFinite(Date.parse(createdAtRaw))
      ? new Date(createdAtRaw).toISOString()
      : new Date().toISOString();

  const removedAtRaw = record.removedAt;
  const removedAt =
    typeof removedAtRaw === "string" && Number.isFinite(Date.parse(removedAtRaw))
      ? new Date(removedAtRaw).toISOString()
      : null;

  const id =
    typeof record.id === "string" && record.id.trim().length > 0
      ? record.id
      : newNoteId();

  return { id, text, createdAt, removedAt };
}

export function serializePaymentLineNotes(
  notes: readonly PaymentLineNoteItem[],
): string | null {
  if (notes.length === 0) return null;
  return JSON.stringify(
    notes.map((note) => ({
      id: note.id,
      text: note.text.trim(),
      createdAt: note.createdAt,
      removedAt: note.removedAt,
    })),
  );
}

export function getActivePaymentLineNotes(
  notes: readonly PaymentLineNoteItem[],
): PaymentLineNoteItem[] {
  return notes.filter((note) => note.removedAt == null);
}

export function softRemovePaymentLineNote(
  notes: readonly PaymentLineNoteItem[],
  noteId: string,
  removedAt = new Date(),
): PaymentLineNoteItem[] {
  return notes.map((note) =>
    note.id === noteId && note.removedAt == null
      ? { ...note, removedAt: removedAt.toISOString() }
      : note,
  );
}

export function formatPaymentLineNoteDate(iso: string): string {
  const date = new Date(iso);
  if (!Number.isFinite(date.getTime())) return "";
  return date.toLocaleDateString("en-US", {
    month: "short",
    day: "numeric",
    year: "numeric",
  });
}

export function formatPaymentLineNotesPreview(
  raw: string | null | undefined,
): string {
  const active = getActivePaymentLineNotes(parsePaymentLineNotes(raw));
  if (active.length === 0) return "";
  if (active.length === 1) {
    return `${formatPaymentLineNoteDate(active[0].createdAt)} · ${active[0].text}`;
  }
  return `${active.length} notes · ${active[0].text}`;
}

export function hasActivePaymentLineNotes(
  raw: string | null | undefined,
): boolean {
  return getActivePaymentLineNotes(parsePaymentLineNotes(raw)).length > 0;
}
