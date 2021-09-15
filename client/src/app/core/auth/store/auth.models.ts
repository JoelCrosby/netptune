import { HttpErrorResponse } from '@angular/common/http';

export const authFeatureKey = 'auth';

export interface AuthFeatureState {
  [authFeatureKey]: AuthState;
}

export interface AuthState {
  token?: UserToken;
  currentUser?: UserResponse;
  isAuthenticated: boolean;
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

export interface LoginRequest {
  email: string;
  password: string;
}

export interface UserResponse {
  userId: string;
  email: string;
  displayName: string;
  pictureUrl: string;
}

export interface UserToken extends UserResponse {
  [key: string]: unknown;
  expires: Date;
  token: string;
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
  | 'requestPasswordResetError'
  | 'resetPasswordError';
