export interface AuthState {
  isAuthenticated: boolean;
  loading: boolean;
  currentUser?: User;
}

export const initialState: AuthState = {
  isAuthenticated: false,
  loading: false,
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
  username: string;
  given_name: string;
  family_name: string;
  picture: string;
  zoneinfo: string;
  expires: Date;
  token: string;
  [key: string]: any;
}
