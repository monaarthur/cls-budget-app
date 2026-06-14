import { apiGet, apiPost, apiPut } from "@/lib/api/client";
import type {
  CreatePayScheduleRequest,
  PayFrequencyTypeResponse,
  PayScheduleDatesResponse,
  PayScheduleResponse,
  UpdatePayScheduleRequest,
} from "@/features/pay-schedules/types/paySchedule";

const paySchedulesPath = "/api/v1/pay-schedules";
const payFrequencyTypesPath = "/api/v1/pay-frequency-types";

export const paySchedulesApi = {
  getFrequencyTypes: () =>
    apiGet<PayFrequencyTypeResponse[]>(payFrequencyTypesPath),
  getAll: () => apiGet<PayScheduleResponse[]>(paySchedulesPath),
  getDefault: () => apiGet<PayScheduleResponse>(`${paySchedulesPath}/default`),
  getById: (id: number) => apiGet<PayScheduleResponse>(`${paySchedulesPath}/${id}`),
  getDates: (id: number, start: string, end: string) =>
    apiGet<PayScheduleDatesResponse>(
      `${paySchedulesPath}/${id}/dates?start=${encodeURIComponent(start)}&end=${encodeURIComponent(end)}`,
    ),
  create: (body: CreatePayScheduleRequest) =>
    apiPost<PayScheduleResponse, CreatePayScheduleRequest>(
      paySchedulesPath,
      body,
    ),
  update: (id: number, body: UpdatePayScheduleRequest) =>
    apiPut<PayScheduleResponse, UpdatePayScheduleRequest>(
      `${paySchedulesPath}/${id}`,
      body,
    ),
};
