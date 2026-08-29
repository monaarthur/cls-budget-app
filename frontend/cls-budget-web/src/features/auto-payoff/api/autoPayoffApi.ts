import { apiGet, apiPost, apiPut } from "@/lib/api/client";
import type {
  AutoPayoffConfig,
  AutoPayoffPreview,
  SaveAutoPayoffConfigRequest,
} from "@/features/auto-payoff/types";

const configPath = "/api/v1/auto-payoff/config";
const configsPath = "/api/v1/auto-payoff/configs";
const previewPath = "/api/v1/auto-payoff/preview";

export const autoPayoffApi = {
  getAll: () => apiGet<AutoPayoffConfig[]>(configsPath),
  getConfig: () => apiGet<AutoPayoffConfig>(configPath),
  getConfigById: (id: number) =>
    apiGet<AutoPayoffConfig>(`${configPath}/${id}`),
  saveConfig: (body: SaveAutoPayoffConfigRequest) =>
    apiPut<AutoPayoffConfig, SaveAutoPayoffConfigRequest>(configPath, body),
  preview: (body: SaveAutoPayoffConfigRequest) =>
    apiPost<AutoPayoffPreview, SaveAutoPayoffConfigRequest>(previewPath, body),
};
