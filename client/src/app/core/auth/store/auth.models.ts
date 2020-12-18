import { HttpErrorResponse } from '@angular/common/http';
import { ClientResponse } from '@core/models/client-response';

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
  requestPasswordReset?: ClientResponse;
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
  email_verified: boolean;
  name: string;
  given_name: string;
  family_name: string;
  picture: string;
  zoneinfo: string;
  expires: Date;
  token: string;
  [key: string]: unknown;
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
