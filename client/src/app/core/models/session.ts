import { UserPermissions } from '@core/models/user-permissions';

export interface UserResponse {
  userId: string;
  email: string;
  displayName: string;
  pictureUrl: string;
  userPermissions?: UserPermissions;
}

export interface LoginResponse extends UserResponse {
  expires: string;
}

export interface AuthCodeRequest {
  userId: string;
  code: string;
}

export interface ResetPasswordRequest {
  userId: string;
  code: string;
  password: string;
}

export interface LinkProviderRequest {
  token: string;
}

export interface WorkspaceInvite {
  email?: string;
  workspaceId?: string;
  code?: string;
  success: boolean;
}
