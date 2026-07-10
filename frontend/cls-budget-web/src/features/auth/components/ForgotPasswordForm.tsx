"use client";

import Link from "next/link";
import { useState } from "react";
import { authApi } from "@/features/auth/api/authApi";
import { Card } from "@/components/ui/Card";
import { ApiError } from "@/lib/api/client";

export function ForgotPasswordForm() {
  const [email, setEmail] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [sent, setSent] = useState(false);

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    setSubmitting(true);
    setError(null);

    try {
      await authApi.forgotPassword({ email: email.trim() });
      setSent(true);
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.message
          : err instanceof Error
            ? err.message
            : "Request failed";
      setError(message);
    } finally {
      setSubmitting(false);
    }
  }

  if (sent) {
    return (
      <Card className="w-full max-w-md p-8 shadow-sm">
        <h1 className="text-2xl font-bold tracking-tight">Check your email</h1>
        <p className="mt-4 text-sm text-[var(--muted)]">
          If an account exists for <strong>{email}</strong>, we sent a password
          reset link. The link expires in about an hour.
        </p>
        <p className="mt-4 text-sm text-[var(--muted)]">
          Check your inbox and spam folder. If nothing arrives, ask your admin
          to confirm SMTP is configured on the API.
        </p>
        <p className="mt-6 text-center text-sm">
          <Link href="/login" className="font-medium text-[var(--link)]">
            Back to sign in
          </Link>
        </p>
      </Card>
    );
  }

  return (
    <Card className="w-full max-w-md p-8 shadow-sm">
      <h1 className="text-2xl font-bold tracking-tight">Forgot password</h1>
      <p className="mt-2 text-sm text-[var(--muted)]">
        Enter your email and we&apos;ll send a reset link.
      </p>

      <form onSubmit={handleSubmit} className="mt-8 flex flex-col gap-4">
        <label className="flex flex-col gap-1.5 text-sm font-medium">
          Email
          <input
            type="email"
            autoComplete="email"
            required
            value={email}
            onChange={(e) => setEmail(e.target.value)}
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
          {submitting ? "Sending…" : "Send reset link"}
        </button>
      </form>

      <p className="mt-6 text-center text-sm text-[var(--muted)]">
        <Link href="/login" className="font-medium text-[var(--link)]">
          Back to sign in
        </Link>
      </p>
    </Card>
  );
}
