import { Service, signal } from '@angular/core';

export const MIN_PAGE_SIDEBAR_WIDTH = 200;
export const MAX_PAGE_SIDEBAR_WIDTH = 520;

const DEFAULT_PAGE_SIDEBAR_WIDTH = 272;
const storageKey = 'netptune.pages.sidebarWidth';

@Service()
export class PageSidebarService {
  readonly width = signal(readWidth());

  private resizing = false;

  setWidth(width: number) {
    this.width.set(clampWidth(width));

    if (this.resizing) return;

    writeWidth(this.width());
  }

  setResizing(resizing: boolean) {
    this.resizing = resizing;

    if (resizing) return;

    writeWidth(this.width());
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

function readWidth(): number {
  try {
    const stored = localStorage.getItem(storageKey);

    return stored === null
      ? DEFAULT_PAGE_SIDEBAR_WIDTH
      : clampWidth(Number(stored));
  } catch {
    return DEFAULT_PAGE_SIDEBAR_WIDTH;
  }
}

function writeWidth(width: number) {
  try {
    localStorage.setItem(storageKey, String(width));
  } catch {
    return;
  }
}
