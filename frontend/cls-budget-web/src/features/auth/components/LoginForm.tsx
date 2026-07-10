"use client";

import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useState } from "react";
import { useAuth } from "@/features/auth/hooks/useAuth";
import { Card } from "@/components/ui/Card";
import { ApiError } from "@/lib/api/client";

export function LoginForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const { login } = useAuth();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const passwordReset = searchParams.get("reset") === "1";
  const welcome = searchParams.get("welcome") === "1";

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    setSubmitting(true);
    setError(null);

    try {
      await login({ email: email.trim(), password });
      const returnUrl = searchParams.get("returnUrl") ?? "/";
      router.replace(returnUrl);
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.message
          : err instanceof Error
            ? err.message
            : "Login failed";
      setError(message);
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <Card className="w-full max-w-md p-8 shadow-sm">
      <h1 className="text-2xl font-bold tracking-tight">Sign in</h1>
      <p className="mt-2 text-sm text-[var(--muted)]">
        Access your household budget.
      </p>

      {passwordReset ? (
        <p className="mt-4 rounded-xl bg-[var(--accent-soft)] px-3 py-2 text-sm text-[var(--foreground)]">
          Password updated. Sign in with your new password.
        </p>
      ) : null}

      {welcome ? (
        <p className="mt-4 rounded-xl bg-[var(--accent-soft)] px-3 py-2 text-sm text-[var(--foreground)]">
          Account created. Sign in with the email and password you just set.
        </p>
      ) : null}

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

        <label className="flex flex-col gap-1.5 text-sm font-medium">
          Password
          <input
            type="password"
            autoComplete="current-password"
            required
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            className="rounded-xl border border-[var(--border)] bg-white px-3 py-2.5 font-normal outline-none focus:ring-2 focus:ring-[var(--accent-soft)]"
          />
        </label>

        <p className="text-right text-sm">
          <Link href="/forgot-password/" className="font-medium text-[var(--link)]">
            Forgot password?
          </Link>
        </p>

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
          {submitting ? "Signing in…" : "Sign in"}
        </button>
      </form>

      <p className="mt-6 text-center text-sm text-[var(--muted)]">
        New here?{" "}
        <Link href="/register" className="font-medium text-[var(--link)]">
          Create an account
        </Link>
      </p>
    </Card>
  );
}
