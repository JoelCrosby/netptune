import { HttpErrorResponse } from '@angular/common/http';

export interface AuthState {
  isAuthenticated: boolean;
  loading: boolean;
  currentUser?: User;
  confirmEmailLoading: boolean;
  loginError?: HttpErrorResponse | Error;
}

export const initialState: AuthState = {
  isAuthenticated: false,
  loading: false,
  confirmEmailLoading: false,
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
  zoneinfo: string;
  expires: Date;
  token: string;
  [key: string]: unknown;
}

export interface ConfirmEmailRequest {
  userId: string;
  code: string;
}
