export type SnackbarType = 'default' | 'success' | 'error' | 'warn' | 'info';

export interface SnackbarConfig {
  duration?: number;
  type?: SnackbarType;
  onAction?: () => void;
}

export interface SnackbarItem {
  id: number;
  message: string;
  action: string | undefined;
  type: SnackbarType;
  duration: number;
  onAction: (() => void) | undefined;
}
