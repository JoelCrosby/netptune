import { HttpErrorResponse } from '@angular/common/http';
import { ClientResponse } from '@app/core/models/client-response';

export interface AuthState {
  currentUser?: User;
  isAuthenticated: boolean;
  loginLoading: boolean;
  loginError?: HttpErrorResponse | Error;
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

export interface User {
  userId: string;
  email: string;
  email_verified: boolean;
  name: string;
  displayName: string;
  given_name: string;
  family_name: string;
  picture: string;
  pictureUrl: string;
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

export type AuthErrorKey =
  | 'loginError'
  | 'registerError'
  | 'requestPasswordResetError'
  | 'requestPasswordResetError'
  | 'resetPasswordError';
