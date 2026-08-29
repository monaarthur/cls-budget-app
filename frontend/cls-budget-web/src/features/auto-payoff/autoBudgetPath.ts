export const AUTO_BUDGET_PATH = "/config/auto-payoff";

export function autoBudgetHref(id?: number | null): string {
  return id && id > 0 ? `${AUTO_BUDGET_PATH}?id=${id}` : AUTO_BUDGET_PATH;
}
