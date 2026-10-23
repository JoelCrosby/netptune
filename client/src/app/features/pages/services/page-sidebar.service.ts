import { Service, inject, signal } from '@angular/core';
import { BrowserSettingsService } from '@core/settings/browser-settings.service';

export const MIN_PAGE_SIDEBAR_WIDTH = 200;
export const MAX_PAGE_SIDEBAR_WIDTH = 520;

const DEFAULT_PAGE_SIDEBAR_WIDTH = 272;

@Service()
export class PageSidebarService {
  private readonly settings = inject(BrowserSettingsService);

  readonly width = signal(this.readWidth());

  private resizing = false;

  setWidth(width: number) {
    this.width.set(clampWidth(width));

    if (this.resizing) return;

    this.writeWidth();
  }

  setResizing(resizing: boolean) {
    this.resizing = resizing;

    if (resizing) return;

    this.writeWidth();
  }

  private readWidth(): number {
    const stored = this.settings.get('pages.sidebarWidth');

    return stored === undefined
      ? DEFAULT_PAGE_SIDEBAR_WIDTH
      : clampWidth(stored);
  }

  private writeWidth() {
    this.settings.set('pages.sidebarWidth', this.width());
  }
}

function clampWidth(width: number): number {
  if (!Number.isFinite(width)) return DEFAULT_PAGE_SIDEBAR_WIDTH;

  const bounded = Math.min(
    MAX_PAGE_SIDEBAR_WIDTH,
    Math.max(MIN_PAGE_SIDEBAR_WIDTH, width)
  );

  return Math.round(bounded);
}
