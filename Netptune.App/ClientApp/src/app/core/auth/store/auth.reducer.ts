import { AuthActions, AuthActionTypes } from './auth.actions';
import { User } from './auth.models';

export interface AuthState {
  isAuthenticated: boolean;
  loading: boolean;
  currentUser: User;
}

export const initialState: AuthState = {
  isAuthenticated: false,
  loading: false,
  currentUser: undefined,
};

export function authReducer(
  state = initialState,
  action: AuthActions
): AuthState {
  switch (action.type) {
    case AuthActionTypes.TRY_LOGIN:
      return { ...state, loading: true };
    case AuthActionTypes.LOGIN_SUCCESS: {
      return {
        ...state,
        isAuthenticated: true,
        loading: false,
        currentUser: action.payload,
      };
    }
    case AuthActionTypes.LOGIN_FAIL:
      return { ...state, isAuthenticated: false, loading: false };
    case AuthActionTypes.LOGOUT:
      return {
        ...state,
        loading: false,
        isAuthenticated: false,
        currentUser: undefined,
      };
    default:
      return state;
  }
}
