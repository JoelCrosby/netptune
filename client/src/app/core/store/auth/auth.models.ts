import { HttpErrorResponse } from '@angular/common/http';
import { UserPermissions } from '@app/core/models/user-permissions';

export const authFeatureKey = 'auth';

export interface AuthFeatureState {
  [authFeatureKey]: AuthState;
}

export interface AuthState {
  currentUser?: UserResponse;
  isAuthenticated: boolean;
  tokenExpires?: string;
  currentUserLoading: boolean;
  currentUserError?: HttpErrorResponse | Error;
  loginLoading: boolean;
  loginError?: boolean;
  registerLoading: boolean;
  registerError?: HttpErrorResponse | Error;
  confirmEmailLoading: boolean;
  confirmEmailLoadingError?: HttpErrorResponse | Error;
  requestPasswordResetLoading: boolean;
  requestPasswordResetError?: HttpErrorResponse | Error;
  resetPasswordLoading: boolean;
  resetPasswordError?: HttpErrorResponse | Error;
}

export const initialState: AuthState = {
  isAuthenticated: false,
  currentUserLoading: false,
  loginLoading: false,
  registerLoading: false,
  confirmEmailLoading: false,
  requestPasswordResetLoading: false,
  resetPasswordLoading: false,
};

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

export interface WorkspaceInvite {
  email?: string;
  workspaceId?: string;
  code?: string;
  success: boolean;
}

export type AuthErrorKey =
  | 'loginError'
  | 'registerError'
  | 'requestPasswordResetError'
  | 'resetPasswordError';
