"use client";

import { useRef, useState, type ChangeEvent } from "react";
import { Upload } from "lucide-react";
import { TopBar } from "@/components/layout/TopBar";
import { Card } from "@/components/ui/Card";
import { TransactionImportGrid } from "@/features/transactions/components/TransactionImportGrid";
import { IncomeSourceSelect } from "@/features/transactions/components/IncomeSourceSelect";
import { transactionImportsApi } from "@/features/transactions/api/transactionImportsApi";
import { incomeSourcesApi } from "@/features/incomes/api/incomeSourcesApi";
import { useIncomeSources } from "@/features/incomes/hooks/useIncomeSources";
import {
  useTransactionImportDetail,
  useTransactionImports,
} from "@/features/transactions/hooks/useTransactionImports";
import type { IncomeSourceResponse } from "@/features/incomes/types/income";
import { ApiError } from "@/lib/api/client";

function formatUploadedAt(iso: string): string {
  return new Date(iso).toLocaleString("en-US", {
    month: "short",
    day: "numeric",
    year: "numeric",
    hour: "numeric",
    minute: "2-digit",
    timeZone: "UTC",
  });
}

export function TransactionsPageContent() {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [selectedImportId, setSelectedImportId] = useState<number | null>(null);
  const [uploading, setUploading] = useState(false);
  const [uploadError, setUploadError] = useState<string | null>(null);
  const [uploadSuccess, setUploadSuccess] = useState<string | null>(null);
  const [updatingImport, setUpdatingImport] = useState(false);
  const [importError, setImportError] = useState<string | null>(null);

  const { imports, loading, error, reload } = useTransactionImports();
  const {
    sources: incomeSources,
    loading: incomeSourcesLoading,
    error: incomeSourcesError,
    reload: reloadIncomeSources,
  } = useIncomeSources();
  const { detail, loading: detailLoading, error: detailError, reload: reloadDetail } =
    useTransactionImportDetail(selectedImportId);

  const handleUploadClick = () => {
    fileInputRef.current?.click();
  };

  const handleFileSelected = async (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    event.target.value = "";
    if (!file) return;

    setUploading(true);
    setUploadError(null);
    setUploadSuccess(null);

    try {
      const result = await transactionImportsApi.upload(file);
      const imported = result.data;
      if (!imported) {
        throw new ApiError(400, "Upload did not return import details.");
      }

      setSelectedImportId(imported.transactionImportId);
      setUploadSuccess(
        `Imported ${imported.rowCount} transaction${imported.rowCount === 1 ? "" : "s"} from ${imported.fileName}.`,
      );
      await reload();
      await reloadDetail();
    } catch (err) {
      setUploadError(
        err instanceof ApiError ? err.message : "Failed to upload CSV file.",
      );
    } finally {
      setUploading(false);
    }
  };

  const handleCreateIncomeSource = async (
    name: string,
  ): Promise<IncomeSourceResponse | null> => {
    const result = await incomeSourcesApi.create(name);
    await reloadIncomeSources();
    return result.data ?? null;
  };

  const handleImportIncomeSourceChange = async (incomeSourceId: number | null) => {
    if (!selectedImportId || detail?.incomeSourceId === incomeSourceId) return;

    setUpdatingImport(true);
    setImportError(null);
    try {
      await transactionImportsApi.updateImport(selectedImportId, { incomeSourceId });
      await reloadDetail();
      await reload();
    } catch (err) {
      setImportError(
        err instanceof ApiError
          ? err.message
          : "Failed to update income source for this import.",
      );
    } finally {
      setUpdatingImport(false);
    }
  };

  return (
    <>
      <TopBar title="Transactions" />

      <Card className="w-full p-5">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h2 className="text-base font-semibold">Upload transactions</h2>
            <p className="mt-1 text-sm text-[var(--muted)]">
              Upload bank or export CSV files to review before applying them to a
              budget.
            </p>
          </div>
          <div className="flex shrink-0 items-center gap-3">
            <input
              ref={fileInputRef}
              type="file"
              accept=".csv,text/csv"
              className="hidden"
              onChange={(event) => void handleFileSelected(event)}
            />
            <button
              type="button"
              onClick={handleUploadClick}
              disabled={uploading}
              className="inline-flex items-center gap-2 rounded-full bg-[var(--accent)] px-5 py-2.5 text-sm font-semibold text-white disabled:opacity-50"
            >
              <Upload size={16} aria-hidden />
              {uploading ? "Uploading…" : "Upload CSV"}
            </button>
          </div>
        </div>
      </Card>

      {uploadError ? (
        <p className="mt-4 text-sm text-[var(--negative)]">{uploadError}</p>
      ) : null}
      {uploadSuccess ? (
        <p className="mt-4 text-sm text-green-700">{uploadSuccess}</p>
      ) : null}
      {error ? (
        <p className="mt-4 text-sm text-[var(--negative)]">{error}</p>
      ) : null}
      {incomeSourcesError ? (
        <p className="mt-4 text-sm text-[var(--negative)]">{incomeSourcesError}</p>
      ) : null}
      {importError ? (
        <p className="mt-4 text-sm text-[var(--negative)]">{importError}</p>
      ) : null}

      <Card className="mt-4 w-full p-4">
        <h2 className="text-sm font-semibold">Imports</h2>
        {loading ? (
          <p className="mt-3 text-sm text-[var(--muted)]">Loading imports…</p>
        ) : imports.length === 0 ? (
          <p className="mt-3 text-sm text-[var(--muted)]">
            No imports yet. Upload a CSV to get started.
          </p>
        ) : (
          <div className="mt-3 flex flex-wrap gap-2">
            {imports.map((item) => {
              const active = item.transactionImportId === selectedImportId;
              return (
                <button
                  key={item.transactionImportId}
                  type="button"
                  onClick={() => setSelectedImportId(item.transactionImportId)}
                  className={`rounded-xl border px-4 py-3 text-left transition ${
                    active
                      ? "border-[var(--link)] bg-[var(--accent-soft)]"
                      : "border-[var(--border)] bg-white hover:bg-slate-50"
                  }`}
                >
                  <p className="text-sm font-medium">{item.fileName}</p>
                  <p className="mt-1 text-xs text-[var(--muted)]">
                    {formatUploadedAt(item.uploadedAt)} · {item.rowCount} rows
                    {item.incomeSourceName ? ` · ${item.incomeSourceName}` : ""}
                  </p>
                </button>
              );
            })}
          </div>
        )}
      </Card>

      {selectedImportId != null ? (
        <div className="mt-4 w-full space-y-4">
          {detail ? (
            <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
              <div>
                <h2 className="text-lg font-semibold">{detail.fileName}</h2>
                <p className="mt-1 text-sm text-[var(--muted)]">
                  Uploaded {formatUploadedAt(detail.uploadedAt)} ·{" "}
                  {detail.rowCount} transactions
                </p>
              </div>
              <label className="block min-w-[240px] text-sm">
                <span className="mb-1 block text-xs font-medium text-[var(--muted)]">
                  Income source for this import
                </span>
                <IncomeSourceSelect
                  sources={incomeSources}
                  value={detail.incomeSourceId}
                  disabled={updatingImport || incomeSourcesLoading}
                  onChange={(incomeSourceId) =>
                    void handleImportIncomeSourceChange(incomeSourceId)
                  }
                  onCreateSource={handleCreateIncomeSource}
                />
              </label>
            </div>
          ) : null}
          {detailError ? (
            <p className="text-sm text-[var(--negative)]">{detailError}</p>
          ) : null}
          <TransactionImportGrid
            transactions={detail?.transactions ?? []}
            loading={detailLoading || incomeSourcesLoading}
          />
        </div>
      ) : null}
    </>
  );
}
