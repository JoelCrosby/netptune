export interface BannerConfig {
  action?: string;
  onAction?: () => void;
  dismissible?: boolean;
}

export interface BannerState {
  message: string;
  action: string | undefined;
  onAction: (() => void) | undefined;
  dismissible: boolean;
}
