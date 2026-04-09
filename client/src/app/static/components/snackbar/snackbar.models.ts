export type SnackbarType = 'default' | 'success' | 'error' | 'warn' | 'info';

export interface SnackbarConfig {
  duration?: number;
  type?: SnackbarType;
}

export interface SnackbarItem {
  id: number;
  message: string;
  action: string | undefined;
  type: SnackbarType;
  duration: number;
}
