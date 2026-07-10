export interface TransactionImportSummary {
  transactionImportId: number;
  fileName: string;
  uploadedAt: string;
  rowCount: number;
  incomeSourceId: number | null;
  incomeSourceName: string | null;
}

export interface ImportedTransaction {
  importedTransactionId: number;
  lineNumber: number;
  description: string;
  categoryRaw: string | null;
  accountCategoryId: number | null;
  accountCategoryName: string | null;
  amount: number;
  transactionDate: string | null;
  postingStatusRaw: string | null;
  budgetPaymentStatusId: number;
  budgetPaymentStatusName: string;
  incomeSourceId: number | null;
  incomeSourceName: string | null;
  notes: string | null;
}

export interface TransactionImportDetail extends TransactionImportSummary {
  transactions: ImportedTransaction[];
}
