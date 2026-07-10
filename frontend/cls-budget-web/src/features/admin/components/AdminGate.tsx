"use client";

import { useState } from "react";
import { adminApi } from "@/features/admin/api/adminApi";
import { setAdminApiKey } from "@/features/admin/lib/adminStorage";
import { Card } from "@/components/ui/Card";
import { ApiError } from "@/lib/api/client";

type AdminGateProps = {
  onAuthenticated: () => void;
};

export function AdminGate({ onAuthenticated }: AdminGateProps) {
  const [apiKey, setApiKey] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    setSubmitting(true);
    setError(null);

    const trimmed = apiKey.trim();
    if (!trimmed) {
      setError("Enter the admin API key.");
      setSubmitting(false);
      return;
    }

    setAdminApiKey(trimmed);

    try {
      await adminApi.listTenants();
      onAuthenticated();
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.message
          : err instanceof Error
            ? err.message
            : "Authentication failed";
      setError(message);
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <Card className="w-full max-w-md p-8 shadow-sm">
      <h1 className="text-2xl font-bold tracking-tight">Admin sign in</h1>
      <p className="mt-2 text-sm text-[var(--muted)]">
        Enter the platform admin API key from{" "}
        <code className="text-xs">Admin:ApiKey</code> in API configuration.
      </p>

      <form onSubmit={handleSubmit} className="mt-8 flex flex-col gap-4">
        <label className="flex flex-col gap-1.5 text-sm font-medium">
          Admin API key
          <input
            type="password"
            autoComplete="off"
            required
            value={apiKey}
            onChange={(e) => setApiKey(e.target.value)}
            className="rounded-xl border border-[var(--border)] bg-white px-3 py-2.5 font-normal outline-none focus:ring-2 focus:ring-[var(--accent-soft)]"
          />
        </label>

        {error ? (
          <p className="text-sm text-[var(--negative)]" role="alert">
            {error}
          </p>
        ) : null}

        <button
          type="submit"
          disabled={submitting}
          className="mt-2 rounded-xl bg-[var(--accent)] px-4 py-2.5 text-sm font-semibold text-white transition hover:opacity-90 disabled:opacity-60"
        >
          {submitting ? "Checking…" : "Continue"}
        </button>
      </form>
    </Card>
  );
}
