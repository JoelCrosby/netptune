import { Injectable, signal } from '@angular/core';
import { BannerConfig, BannerState } from './banner.models';

@Injectable({
  providedIn: 'root',
})
export class BannerService {
  readonly banner = signal<BannerState | null>(null);

  show(message: string, config?: BannerConfig): void {
    this.banner.set({
      message,
      action: config?.action,
      onAction: config?.onAction,
      dismissible: config?.dismissible ?? true,
    });
  }

  dismiss(): void {
    this.banner.set(null);
  }
}
