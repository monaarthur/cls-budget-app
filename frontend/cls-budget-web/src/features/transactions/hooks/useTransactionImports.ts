"use client";

import { useCallback, useEffect, useState } from "react";
import { transactionImportsApi } from "@/features/transactions/api/transactionImportsApi";
import type {
  TransactionImportDetail,
  TransactionImportSummary,
} from "@/features/transactions/types/transactionImport";
import { ApiError } from "@/lib/api/client";

export function useTransactionImports() {
  const [imports, setImports] = useState<TransactionImportSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await transactionImportsApi.getAll();
      setImports(result.data ?? []);
    } catch (err) {
      setError(
        err instanceof ApiError
          ? err.message
          : "Failed to load transaction imports",
      );
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  return { imports, loading, error, reload: load };
}

export function useTransactionImportDetail(importId: number | null) {
  const [detail, setDetail] = useState<TransactionImportDetail | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    if (importId == null) {
      setDetail(null);
      return;
    }

    setLoading(true);
    setError(null);
    try {
      const result = await transactionImportsApi.getById(importId);
      setDetail(result.data ?? null);
    } catch (err) {
      setError(
        err instanceof ApiError
          ? err.message
          : "Failed to load transaction import",
      );
    } finally {
      setLoading(false);
    }
  }, [importId]);

  useEffect(() => {
    void load();
  }, [load]);

  return { detail, loading, error, reload: load };
}
