import { Permission } from '@core/auth/permissions';

export interface ApiCredential {
  id: string;
  name: string;
  tokenPrefix: string;
  createdAt: string;
  expiresAt: string;
  revokedAt?: string;
  lastUsedAt?: string;
  scopes: Permission[];
}

export interface ServiceAccount {
  id: number;
  userId: string;
  name: string;
  description?: string;
  createdAt: string;
  disabledAt?: string;
  ownerUserIds: string[];
  permissions: Permission[];
  credentials: ApiCredential[];
}

export interface CreateServiceAccountRequest {
  name: string;
  description?: string;
  permissions: Permission[];
  ownerUserIds: string[];
}

export interface CreateApiCredentialRequest {
  name: string;
  scopes: Permission[];
  expiresAt?: string;
}

export interface ApiCredentialCreated {
  id: string;
  name: string;
  token: string;
  expiresAt: string;
  scopes: Permission[];
}
