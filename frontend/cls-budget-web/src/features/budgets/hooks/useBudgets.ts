"use client";

import { useCallback, useEffect, useState } from "react";
import { budgetsApi } from "@/features/budgets/api/budgetsApi";
import type { BudgetResponse } from "@/features/budgets/types/budget";
import { ApiError } from "@/lib/api/client";

export function useBudgets() {
  const [budgets, setBudgets] = useState<BudgetResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await budgetsApi.getAll();
      setBudgets(result.data ?? []);
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.message
          : err instanceof Error
            ? err.message
            : "Failed to load budgets";
      setError(message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  return { budgets, loading, error, reload: load };
}
