import { effect, Injectable, signal } from '@angular/core';

const THEME_STORAGE_KEY = 'Netptune-settings.theme';
const themes = new Set(['light', 'dark']);

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly current = signal(readStoredTheme());

  readonly theme = this.current.asReadonly();

  constructor() {
    effect(() => this.apply(this.theme()));
  }

  set(theme: string) {
    this.current.set(theme.toLowerCase());
  }

  private apply(theme: string) {
    const classList = document.documentElement.classList;
    const applied = Array.from(classList).filter((item) => themes.has(item));

    if (applied.length) {
      classList.remove(...applied);
    }

    classList.add(theme);

    try {
      localStorage.setItem(THEME_STORAGE_KEY, JSON.stringify(theme));
    } catch {
      // Storage can be unavailable (private mode, quota). The theme is
      // already applied; only the pre-paint cache is lost.
    }
  }
}

// index.html reads the same key before first paint, so the two have to agree.
function readStoredTheme(): string {
  try {
    const stored = localStorage.getItem(THEME_STORAGE_KEY);
    const theme = stored ? (JSON.parse(stored) as string) : null;

    return theme && themes.has(theme) ? theme : 'light';
  } catch {
    return 'light';
  }
}
