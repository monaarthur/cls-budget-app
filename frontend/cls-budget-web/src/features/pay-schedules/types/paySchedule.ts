export const PayFrequencyTypeIds = {
  Weekly: 1,
  BiWeekly: 2,
  SemiMonthly: 3,
} as const;

export interface PayFrequencyTypeResponse {
  payFrequencyTypeId: number;
  name: string;
  description: string | null;
}

export interface PayScheduleResponse {
  payScheduleId: number;
  incomeSourceId: number;
  incomeSourceName: string;
  payFrequencyTypeId: number;
  payFrequencyTypeName: string;
  name: string;
  anchorDate: string | null;
  dayOfWeek: number | null;
  semiMonthlyDay1: number | null;
  semiMonthlyDay2: number | null;
  isDefault: boolean;
  isActive: boolean;
}

export interface CreatePayScheduleRequest {
  incomeSourceId: number;
  payFrequencyTypeId: number;
  name: string;
  anchorDate?: string | null;
  dayOfWeek?: number | null;
  semiMonthlyDay1?: number | null;
  semiMonthlyDay2?: number | null;
  isDefault: boolean;
  isActive?: boolean;
}

export type UpdatePayScheduleRequest = CreatePayScheduleRequest;

export interface PayPeriodBoundary {
  periodStart: string;
  periodEnd: string;
  label: string;
}

export interface PayScheduleDatesResponse {
  payScheduleId: number;
  rangeStart: string;
  rangeEnd: string;
  payDates: string[];
  periods: PayPeriodBoundary[];
}

export interface PayScheduleConfig {
  payFrequencyTypeId: number;
  anchorDate: string | null;
  semiMonthlyDay1: number | null;
  semiMonthlyDay2: number | null;
}
