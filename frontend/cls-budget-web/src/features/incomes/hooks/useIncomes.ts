"use client";

import { useCallback, useEffect, useState } from "react";
import { incomesApi } from "@/features/incomes/api/incomesApi";
import type { IncomeResponse } from "@/features/incomes/types/income";
import { ApiError } from "@/lib/api/client";

export function useIncomes(budgetId?: number | null) {
  const [incomes, setIncomes] = useState<IncomeResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const result =
        budgetId != null
          ? await incomesApi.getByBudget(budgetId)
          : await incomesApi.getAll();
      setIncomes(result.data ?? []);
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.message
          : err instanceof Error
            ? err.message
            : "Failed to load income";
      setError(message);
    } finally {
      setLoading(false);
    }
  }, [budgetId]);

  useEffect(() => {
    void load();
  }, [load]);

  return { incomes, loading, error, reload: load };
}
