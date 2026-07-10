"use client";

import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useState } from "react";
import { authApi } from "@/features/auth/api/authApi";
import { Card } from "@/components/ui/Card";
import { ApiError } from "@/lib/api/client";

export function ResetPasswordForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const token = searchParams.get("token") ?? "";
  const isInvite = searchParams.get("invite") === "1";

  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);

    if (!token) {
      setError("Missing reset token. Use the link from your email.");
      return;
    }

    if (password !== confirmPassword) {
      setError("Passwords do not match.");
      return;
    }

    setSubmitting(true);
    try {
      await authApi.resetPassword({ token, newPassword: password });
      router.replace(isInvite ? "/login?welcome=1" : "/login?reset=1");
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.message
          : err instanceof Error
            ? err.message
            : "Reset failed";
      setError(message);
    } finally {
      setSubmitting(false);
    }
  }

  if (!token) {
    return (
      <Card className="w-full max-w-md p-8 shadow-sm">
        <h1 className="text-2xl font-bold tracking-tight">Invalid link</h1>
        <p className="mt-4 text-sm text-[var(--muted)]">
          This password reset link is missing a token. Request a new link below.
        </p>
        <p className="mt-6 text-center text-sm">
          <Link href="/forgot-password/" className="font-medium text-[var(--link)]">
            Request reset link
          </Link>
        </p>
      </Card>
    );
  }

  return (
    <Card className="w-full max-w-md p-8 shadow-sm">
      <h1 className="text-2xl font-bold tracking-tight">
        {isInvite ? "Create your account" : "Set new password"}
      </h1>
      <p className="mt-2 text-sm text-[var(--muted)]">
        {isInvite
          ? "Choose a password to finish setting up your CLS Budget account."
          : "Choose a new password for your account."}
      </p>

      <form onSubmit={handleSubmit} className="mt-8 flex flex-col gap-4">
        <label className="flex flex-col gap-1.5 text-sm font-medium">
          New password
          <input
            type="password"
            autoComplete="new-password"
            required
            minLength={8}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            className="rounded-xl border border-[var(--border)] bg-white px-3 py-2.5 font-normal outline-none focus:ring-2 focus:ring-[var(--accent-soft)]"
          />
        </label>

        <label className="flex flex-col gap-1.5 text-sm font-medium">
          Confirm password
          <input
            type="password"
            autoComplete="new-password"
            required
            minLength={8}
            value={confirmPassword}
            onChange={(e) => setConfirmPassword(e.target.value)}
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
          {submitting ? "Saving…" : isInvite ? "Create account" : "Update password"}
        </button>
      </form>
    </Card>
  );
}
