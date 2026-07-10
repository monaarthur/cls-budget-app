const ADMIN_API_KEY_STORAGE = "cls_budget_admin_api_key";
const ADMIN_SETUP_LINK_STORAGE = "cls_budget_admin_setup_link";
const ADMIN_SETUP_MESSAGE_STORAGE = "cls_budget_admin_setup_message";

export function getAdminApiKey(): string | null {
  if (typeof window === "undefined") return null;
  return sessionStorage.getItem(ADMIN_API_KEY_STORAGE);
}

export function setAdminApiKey(apiKey: string): void {
  sessionStorage.setItem(ADMIN_API_KEY_STORAGE, apiKey.trim());
}

export function clearAdminApiKey(): void {
  sessionStorage.removeItem(ADMIN_API_KEY_STORAGE);
}

export function getAdminSetupLink(): string | null {
  if (typeof window === "undefined") return null;
  return sessionStorage.getItem(ADMIN_SETUP_LINK_STORAGE);
}

export function getAdminSetupMessage(): string | null {
  if (typeof window === "undefined") return null;
  return sessionStorage.getItem(ADMIN_SETUP_MESSAGE_STORAGE);
}

export function setAdminSetupLink(link: string, message?: string): void {
  sessionStorage.setItem(ADMIN_SETUP_LINK_STORAGE, link.trim());
  if (message) {
    sessionStorage.setItem(ADMIN_SETUP_MESSAGE_STORAGE, message);
  }
}

export function clearAdminSetupLink(): void {
  sessionStorage.removeItem(ADMIN_SETUP_LINK_STORAGE);
  sessionStorage.removeItem(ADMIN_SETUP_MESSAGE_STORAGE);
}
