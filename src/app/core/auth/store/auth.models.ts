import { HttpErrorResponse } from '@angular/common/http';

export interface AuthState {
  isAuthenticated: boolean;
  loading: boolean;
  currentUser?: User;
  confirmEmailLoading: boolean;
  requestPasswordResetLoading: boolean;
  loginError?: HttpErrorResponse | Error;
  requestPasswordResetError?: HttpErrorResponse | Error;
}

export const initialState: AuthState = {
  isAuthenticated: false,
  loading: false,
  confirmEmailLoading: false,
  requestPasswordResetLoading: false,
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
