"use client";

import { useCallback, useEffect, useState } from "react";
import { incomeSourcesApi } from "@/features/incomes/api/incomeSourcesApi";
import type { IncomeSourceResponse } from "@/features/incomes/types/income";
import { ApiError } from "@/lib/api/client";

export function useIncomeSources() {
  const [sources, setSources] = useState<IncomeSourceResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await incomeSourcesApi.getAll();
      setSources((result.data ?? []).filter((source) => source.isActive));
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.message
          : err instanceof Error
            ? err.message
            : "Failed to load income sources";
      setError(message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  return { sources, loading, error, reload: load };
}
