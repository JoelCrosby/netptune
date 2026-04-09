import { Injectable, signal } from '@angular/core';
import { SnackbarConfig, SnackbarItem } from './snackbar.models';

@Injectable({
  providedIn: 'root',
})
export class SnackbarService {
  readonly items = signal<SnackbarItem[]>([]);

  private nextId = 0;

  open(message: string, action?: string, config?: SnackbarConfig): SnackbarItem {
    const item: SnackbarItem = {
      id: this.nextId++,
      message,
      action,
      type: config?.type ?? 'default',
      duration: config?.duration ?? 3000,
    };

    this.items.update((current) => [...current, item]);

    if (item.duration > 0) {
      setTimeout(() => this.dismiss(item.id), item.duration);
    }

    return item;
  }

  success(message: string, config?: Omit<SnackbarConfig, 'type'>): SnackbarItem {
    return this.open(message, undefined, { ...config, type: 'success' });
  }

  error(message: string, config?: Omit<SnackbarConfig, 'type'>): SnackbarItem {
    return this.open(message, undefined, { ...config, type: 'error', duration: config?.duration ?? 5000 });
  }

  warn(message: string, config?: Omit<SnackbarConfig, 'type'>): SnackbarItem {
    return this.open(message, undefined, { ...config, type: 'warn' });
  }

  info(message: string, config?: Omit<SnackbarConfig, 'type'>): SnackbarItem {
    return this.open(message, undefined, { ...config, type: 'info' });
  }

  dismiss(id: number): void {
    this.items.update((current) => current.filter((item) => item.id !== id));
  }
}
