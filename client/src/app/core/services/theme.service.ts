import { effect, Injectable, signal } from '@angular/core';

const THEME_STORAGE_KEY = 'Netptune-settings.theme';
const themes = new Set(['light', 'dark']);

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly current = signal(initialTheme());

  readonly theme = this.current.asReadonly();

  constructor() {
    effect(() => this.apply(this.theme()));
  }

  set(theme: string) {
    const chosen = theme.toLowerCase();

    this.current.set(chosen);
    this.remember(chosen);
  }

  /** Nothing is chosen any more, so the browser's own preference decides again. */
  clear() {
    this.forget();
    this.current.set(browserTheme());
  }

  private apply(theme: string) {
    const classList = document.documentElement.classList;
    const applied = Array.from(classList).filter((item) => themes.has(item));

    if (applied.length) {
      classList.remove(...applied);
    }

    classList.add(theme);
  }

  /**
   * Only a chosen theme is cached. A theme that came from the browser is not,
   * so a later change to that preference is picked up rather than overruled by
   * what it happened to be on an earlier visit.
   */
  private remember(theme: string) {
    try {
      localStorage.setItem(THEME_STORAGE_KEY, JSON.stringify(theme));
    } catch {
      // Storage can be unavailable (private mode, quota). The theme is
      // already applied; only the pre-paint cache is lost.
    }
  }

  private forget() {
    try {
      localStorage.removeItem(THEME_STORAGE_KEY);
    } catch {
      // Nothing to undo — the cache simply stays as it was.
    }
  }
}

// index.html reads the same key before first paint, so the two have to agree.
function initialTheme(): string {
  return storedTheme() ?? browserTheme();
}

function storedTheme(): string | null {
  try {
    const stored = localStorage.getItem(THEME_STORAGE_KEY);
    const theme = stored ? (JSON.parse(stored) as string) : null;

    return theme && themes.has(theme) ? theme : null;
  } catch {
    return null;
  }
}

/** Whatever the operating system or browser is already set to. */
function browserTheme(): string {
  const prefersDark = window.matchMedia?.(
    '(prefers-color-scheme: dark)'
  ).matches;

  return prefersDark ? 'dark' : 'light';
}
