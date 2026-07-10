export type TenantRole = "Owner" | "Member";

export interface TenantSummary {
  tenantId: string;
  name: string;
  isActive: boolean;
  userCount: number;
  userEmails: string[];
}

export interface InviteTenantUserRequest {
  tenantId: string;
  email: string;
  displayName: string;
  role: TenantRole;
}

export interface InviteTenantUserResponse {
  userId: string;
  tenantId: string;
  email: string;
  displayName: string;
  inviteSent: boolean;
  setupLink: string;
}
