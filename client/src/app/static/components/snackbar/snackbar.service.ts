import { LiveAnnouncer } from '@angular/cdk/a11y';
import { inject, Service, signal } from '@angular/core';
import { SnackbarConfig, SnackbarItem } from './snackbar.models';

@Service()
export class SnackbarService {
  readonly items = signal<SnackbarItem[]>([]);

  private readonly liveAnnouncer = inject(LiveAnnouncer);
  private readonly timers = new Map<number, ReturnType<typeof setTimeout>>();
  private nextId = 0;

  open(
    message: string,
    action?: string,
    config?: SnackbarConfig
  ): SnackbarItem {
    const item: SnackbarItem = {
      id: this.nextId++,
      message,
      action,
      type: config?.type ?? 'default',
      duration: config?.duration ?? 3000,
      onAction: config?.onAction,
    };

    this.items.update((current) => [...current, item]);
    this.announce(item);
    this.release(item.id);

    return item;
  }

  hold(id: number): void {
    const timer = this.timers.get(id);

    if (timer === undefined) return;

    clearTimeout(timer);
    this.timers.delete(id);
  }

  release(id: number): void {
    const item = this.items().find((candidate) => candidate.id === id);

    if (!item || item.duration <= 0) return;

    this.hold(id);
    this.timers.set(
      id,
      setTimeout(() => this.dismiss(id), item.duration)
    );
  }

  success(
    message: string,
    config?: Omit<SnackbarConfig, 'type'>
  ): SnackbarItem {
    return this.open(message, undefined, { ...config, type: 'success' });
  }

  error(message: string, config?: Omit<SnackbarConfig, 'type'>): SnackbarItem {
    return this.open(message, undefined, {
      ...config,
      type: 'error',
      duration: config?.duration ?? 5000,
    });
  }

  warn(message: string, config?: Omit<SnackbarConfig, 'type'>): SnackbarItem {
    return this.open(message, undefined, { ...config, type: 'warn' });
  }

  info(message: string, config?: Omit<SnackbarConfig, 'type'>): SnackbarItem {
    return this.open(message, undefined, { ...config, type: 'info' });
  }

  dismiss(id: number): void {
    this.hold(id);
    this.items.update((current) => current.filter((item) => item.id !== id));
  }

  private announce(item: SnackbarItem): void {
    const message = item.action
      ? `${item.message} ${item.action}`
      : item.message;
    const politeness = item.type === 'error' ? 'assertive' : 'polite';

    void this.liveAnnouncer.announce(message, politeness);
  }
}
