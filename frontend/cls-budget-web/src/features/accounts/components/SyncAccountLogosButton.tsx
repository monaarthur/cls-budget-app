"use client";

import { useState } from "react";
import { RefreshCw } from "lucide-react";

export function SyncAccountLogosButton({
  onSynced,
}: {
  onSynced?: () => void;
}) {
  const [syncing, setSyncing] = useState(false);
  const [message, setMessage] = useState<string | null>(null);

  const handleSync = async () => {
    setSyncing(true);
    setMessage(null);

    try {
      const response = await fetch("/logo-actions/sync", { method: "POST" });
      const payload = (await response.json()) as {
        saved?: number;
        skipped?: number;
        message?: string;
      };

      if (!response.ok) {
        throw new Error(payload.message ?? "Failed to sync logos");
      }

      setMessage(
        `Saved ${payload.saved ?? 0} logo${payload.saved === 1 ? "" : "s"} to public/account-logos`,
      );
      onSynced?.();
    } catch (error) {
      setMessage(
        error instanceof Error ? error.message : "Failed to sync logos",
      );
    } finally {
      setSyncing(false);
    }
  };

  return (
    <div className="flex flex-wrap items-center gap-3">
      <button
        type="button"
        onClick={() => void handleSync()}
        disabled={syncing}
        className="inline-flex items-center gap-2 rounded-full border border-[var(--border)] bg-white px-4 py-2 text-sm font-medium disabled:opacity-50"
      >
        <RefreshCw
          size={15}
          className={syncing ? "animate-spin" : undefined}
          aria-hidden
        />
        {syncing ? "Syncing logos…" : "Sync logos from URLs"}
      </button>
      {message ? (
        <p className="text-sm text-[var(--muted)]">{message}</p>
      ) : null}
    </div>
  );
}
