"use client";

import { useCallback, useEffect, useState } from "react";
import { accountsApi } from "@/features/accounts/api/accountsApi";
import type { AccountResponse } from "@/features/accounts/types/account";
import { ApiError } from "@/lib/api/client";

export function useAccounts() {
  const [accounts, setAccounts] = useState<AccountResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await accountsApi.getAll();
      setAccounts(result.data ?? []);
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.message
          : err instanceof Error
            ? err.message
            : "Failed to load accounts";
      setError(message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  return { accounts, loading, error, reload: load };
}
