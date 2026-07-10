"use client";

import { useCallback, useEffect, useState } from "react";
import { adminApi } from "@/features/admin/api/adminApi";
import { clearAdminApiKey, clearAdminSetupLink, getAdminApiKey, getAdminSetupLink, getAdminSetupMessage, setAdminSetupLink } from "@/features/admin/lib/adminStorage";
import type { TenantSummary, TenantRole } from "@/features/admin/types/admin";
import { Card } from "@/components/ui/Card";
import { ApiError } from "@/lib/api/client";

type AdminDashboardProps = {
  onSignedOut: () => void;
};

export function AdminDashboard({ onSignedOut }: AdminDashboardProps) {
  const [tenants, setTenants] = useState<TenantSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [selectedTenantId, setSelectedTenantId] = useState("");
  const [email, setEmail] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [role, setRole] = useState<TenantRole>("Owner");
  const [inviting, setInviting] = useState(false);
  const [inviteMessage, setInviteMessage] = useState<string | null>(null);
  const [setupLink, setSetupLink] = useState<string | null>(null);
  const [inviteError, setInviteError] = useState<string | null>(null);

  const loadTenants = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await adminApi.listTenants();
      const list = result.data ?? [];
      setTenants(list);
      setSelectedTenantId((current) => current || list[0]?.tenantId || "");
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.message
          : err instanceof Error
            ? err.message
            : "Failed to load tenants";
      if (err instanceof ApiError && (err.status === 401 || err.status === 503)) {
        clearAdminApiKey();
        onSignedOut();
        return;
      }
      setError(message);
    } finally {
      setLoading(false);
    }
  }, [onSignedOut]);

  useEffect(() => {
    if (getAdminApiKey()) {
      void loadTenants();
    }
    const savedLink = getAdminSetupLink();
    if (savedLink) {
      setSetupLink(savedLink);
      setInviteMessage(getAdminSetupMessage());
    }
  }, [loadTenants]);

  async function handleInvite(event: React.FormEvent) {
    event.preventDefault();
    setInviting(true);
    setInviteMessage(null);
    setSetupLink(null);
    setInviteError(null);

    try {
      const result = await adminApi.inviteUser({
        tenantId: selectedTenantId,
        email: email.trim(),
        displayName: displayName.trim(),
        role,
      });

      const invited = result.data!;
      setInviteMessage(
        `Invite sent for ${invited.email}. Use the setup link shown on screen if email did not arrive.`,
      );
      setSetupLink(invited.setupLink);
      setAdminSetupLink(
        invited.setupLink,
        `Invite sent for ${invited.email}. Use the setup link shown on screen if email did not arrive.`,
      );
      setEmail("");
      setDisplayName("");
      await loadTenants();
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.message
          : err instanceof Error
            ? err.message
            : "Invite failed";
      setInviteError(message);
    } finally {
      setInviting(false);
    }
  }

  function handleSignOut() {
    clearAdminApiKey();
    onSignedOut();
  }

  return (
    <div className="flex w-full max-w-4xl flex-col gap-8">
      <div className="flex items-start justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Admin</h1>
          <p className="mt-2 text-sm text-[var(--muted)]">
            Assign emails to tenants and send account setup links.
          </p>
        </div>
        <button
          type="button"
          onClick={handleSignOut}
          className="rounded-xl border border-[var(--border)] px-3 py-2 text-sm font-medium hover:bg-[var(--accent-soft)]"
        >
          Sign out
        </button>
      </div>

      <Card className="p-6 shadow-sm">
        <h2 className="text-lg font-semibold">Tenants</h2>
        <p className="mt-1 text-sm text-[var(--muted)]">
          Households in the database and their linked user emails.
        </p>

        {loading ? (
          <p className="mt-6 text-sm text-[var(--muted)]">Loading tenants…</p>
        ) : error ? (
          <p className="mt-6 text-sm text-[var(--negative)]" role="alert">
            {error}
          </p>
        ) : tenants.length === 0 ? (
          <p className="mt-6 text-sm text-[var(--muted)]">No tenants found.</p>
        ) : (
          <div className="mt-6 overflow-x-auto">
            <table className="w-full min-w-[640px] text-left text-sm">
              <thead>
                <tr className="border-b border-[var(--border)] text-[var(--muted)]">
                  <th className="py-2 pr-4 font-medium">Name</th>
                  <th className="py-2 pr-4 font-medium">Tenant ID</th>
                  <th className="py-2 pr-4 font-medium">Users</th>
                  <th className="py-2 font-medium">Emails</th>
                </tr>
              </thead>
              <tbody>
                {tenants.map((tenant) => (
                  <tr
                    key={tenant.tenantId}
                    className="border-b border-[var(--border)] last:border-0"
                  >
                    <td className="py-3 pr-4 font-medium">{tenant.name}</td>
                    <td className="py-3 pr-4 font-mono text-xs text-[var(--muted)]">
                      {tenant.tenantId}
                    </td>
                    <td className="py-3 pr-4">{tenant.userCount}</td>
                    <td className="py-3 text-[var(--muted)]">
                      {tenant.userEmails.length > 0
                        ? tenant.userEmails.join(", ")
                        : "—"}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>

      <Card className="p-6 shadow-sm">
        <h2 className="text-lg font-semibold">Invite user to tenant</h2>
        <p className="mt-1 text-sm text-[var(--muted)]">
          Creates an account on the selected tenant and emails a link to set
          their password.
        </p>

        <form onSubmit={handleInvite} className="mt-6 grid gap-4 sm:grid-cols-2">
          <label className="flex flex-col gap-1.5 text-sm font-medium sm:col-span-2">
            Tenant
            <select
              required
              value={selectedTenantId}
              onChange={(e) => setSelectedTenantId(e.target.value)}
              className="rounded-xl border border-[var(--border)] bg-white px-3 py-2.5 font-normal outline-none focus:ring-2 focus:ring-[var(--accent-soft)]"
            >
              {tenants.map((tenant) => (
                <option key={tenant.tenantId} value={tenant.tenantId}>
                  {tenant.name} ({tenant.userCount} user
                  {tenant.userCount === 1 ? "" : "s"})
                </option>
              ))}
            </select>
          </label>

          <label className="flex flex-col gap-1.5 text-sm font-medium">
            Email
            <input
              type="email"
              required
              autoComplete="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="rounded-xl border border-[var(--border)] bg-white px-3 py-2.5 font-normal outline-none focus:ring-2 focus:ring-[var(--accent-soft)]"
            />
          </label>

          <label className="flex flex-col gap-1.5 text-sm font-medium">
            Display name
            <input
              type="text"
              required
              value={displayName}
              onChange={(e) => setDisplayName(e.target.value)}
              className="rounded-xl border border-[var(--border)] bg-white px-3 py-2.5 font-normal outline-none focus:ring-2 focus:ring-[var(--accent-soft)]"
            />
          </label>

          <label className="flex flex-col gap-1.5 text-sm font-medium">
            Role
            <select
              value={role}
              onChange={(e) => setRole(e.target.value as TenantRole)}
              className="rounded-xl border border-[var(--border)] bg-white px-3 py-2.5 font-normal outline-none focus:ring-2 focus:ring-[var(--accent-soft)]"
            >
              <option value="Owner">Owner</option>
              <option value="Member">Member</option>
            </select>
          </label>

          <div className="flex items-end sm:col-span-2">
            <button
              type="submit"
              disabled={inviting || tenants.length === 0}
              className="rounded-xl bg-[var(--accent)] px-4 py-2.5 text-sm font-semibold text-white transition hover:opacity-90 disabled:opacity-60"
            >
              {inviting ? "Sending invite…" : "Send invite"}
            </button>
          </div>
        </form>

        {inviteMessage && !setupLink ? (
          <p className="mt-4 text-sm text-[var(--foreground)]" role="status">
            {inviteMessage}
          </p>
        ) : null}
        {inviteError ? (
          <p className="mt-4 text-sm text-[var(--negative)]" role="alert">
            {inviteError}
          </p>
        ) : null}
      </Card>

      {setupLink ? (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/30 p-4"
          role="dialog"
          aria-labelledby="setup-link-title"
          aria-modal="true"
        >
          <div className="w-full max-w-lg rounded-2xl border border-[var(--border)] bg-white p-6 shadow-lg">
            {inviteMessage ? (
              <p className="text-sm text-[var(--foreground)]" role="status">
                {inviteMessage}
              </p>
            ) : null}
            <p id="setup-link-title" className="mt-3 text-sm font-semibold">
              Account setup link
            </p>
            <p className="mt-2 max-h-24 overflow-y-auto break-all font-mono text-xs text-[var(--muted)]">
              {setupLink}
            </p>
            <div className="mt-5 flex flex-wrap gap-3">
              <a
                href={setupLink}
                target="_blank"
                rel="noreferrer"
                className="rounded-xl bg-[var(--accent)] px-4 py-2.5 text-sm font-semibold text-white transition hover:opacity-90"
              >
                Open link to set password
              </a>
              <button
                type="button"
                onClick={() => {
                  setSetupLink(null);
                  setInviteMessage(null);
                  clearAdminSetupLink();
                }}
                className="rounded-xl border border-[var(--border)] px-4 py-2.5 text-sm font-medium hover:bg-[var(--accent-soft)]"
              >
                Dismiss
              </button>
            </div>
          </div>
        </div>
      ) : null}
    </div>
  );
}
