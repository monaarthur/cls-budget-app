import { useCallback, useEffect, useState } from "react";
import { accountCategoriesApi } from "@/features/accounts/api/accountCategoriesApi";
import type { AccountCategoryResponse } from "@/features/accounts/types/accountCategory";
import { ApiError } from "@/lib/api/client";

export function useAccountCategories() {
  const [categories, setCategories] = useState<AccountCategoryResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await accountCategoriesApi.getAll();
      setCategories(result.data ?? []);
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.message
          : err instanceof Error
            ? err.message
            : "Failed to load categories";
      setError(message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void reload();
  }, [reload]);

  return { categories, loading, error, reload };
}
