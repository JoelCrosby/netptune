import { Service, signal } from '@angular/core';

const storageKey = 'netptune.pages.railCollapsed';

@Service()
export class PageRailService {
  readonly collapsed = signal(readCollapsed());

  setCollapsed(collapsed: boolean) {
    this.collapsed.set(collapsed);
    writeCollapsed(collapsed);
  }
}

function readCollapsed(): boolean {
  try {
    return localStorage.getItem(storageKey) === 'true';
  } catch {
    return false;
  }
}

function writeCollapsed(collapsed: boolean) {
  try {
    localStorage.setItem(storageKey, String(collapsed));
  } catch {
    // Storage can be unavailable (private mode, quota); the rail just won't stay collapsed.
  }
}
