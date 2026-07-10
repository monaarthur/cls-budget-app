"use client";

import { useEffect, useState } from "react";
import { AdminDashboard } from "@/features/admin/components/AdminDashboard";
import { AdminGate } from "@/features/admin/components/AdminGate";
import { getAdminApiKey } from "@/features/admin/lib/adminStorage";

export function AdminPageContent() {
  const [authenticated, setAuthenticated] = useState(false);
  const [ready, setReady] = useState(false);

  useEffect(() => {
    setAuthenticated(!!getAdminApiKey());
    setReady(true);
  }, []);

  if (!ready) {
    return <p className="text-sm text-[var(--muted)]">Loading…</p>;
  }

  if (!authenticated) {
    return <AdminGate onAuthenticated={() => setAuthenticated(true)} />;
  }

  return <AdminDashboard onSignedOut={() => setAuthenticated(false)} />;
}
