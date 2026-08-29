export interface AutoPayoffConfig {
  autoPayoffConfigId?: number | null;
  name: string;
  excludeStatusIds: number[];
  excludeCategoryIds: number[];
  excludeAccountIds: number[];
  targetCategoryIds: number[];
  targetByPayoffDate: boolean;
  extraMonthlyAmount: number;
}

export type SaveAutoPayoffConfigRequest = AutoPayoffConfig;

export interface AutoPayoffPreviewItem {
  rank: number;
  accountId: number;
  name: string;
  categoryId: number;
  categoryName: string;
  balance: number;
  monthlyPayment: number;
  targetDate: string | null;
  reason: string;
  extraAllocated: number;
}

export interface AutoPayoffExcludedItem {
  accountId: number;
  name: string;
  reason: string;
}

export interface AutoPayoffPreview {
  config: AutoPayoffConfig;
  queue: AutoPayoffPreviewItem[];
  excluded: AutoPayoffExcludedItem[];
  extraPayment: number;
  extraAllocated: number;
  extraRemaining: number;
}
