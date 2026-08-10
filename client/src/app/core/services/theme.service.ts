import { effect, Injectable, signal } from '@angular/core';

export type Theme = 'light' | 'dark';

const THEME_STORAGE_KEY = 'Netptune-settings.theme';
const themes = new Set<string>(['light', 'dark']);

function isTheme(value: string): value is Theme {
  return themes.has(value);
}

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly current = signal(initialTheme());

  readonly theme = this.current.asReadonly();

  constructor() {
    effect(() => this.apply(this.theme()));
  }

  set(theme: string) {
    const chosen = theme.toLowerCase();

    if (!isTheme(chosen)) {
      return;
    }

    this.current.set(chosen);
    this.remember(chosen);
  }

  clear() {
    this.forget();
    this.current.set(browserTheme());
  }

  private apply(theme: Theme) {
    const classList = document.documentElement.classList;
    const applied = Array.from(classList).filter((item) => themes.has(item));

    if (applied.length) {
      classList.remove(...applied);
    }

    classList.add(theme);
  }

  private remember(theme: Theme) {
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

function initialTheme(): Theme {
  return storedTheme() ?? browserTheme();
}

function storedTheme(): Theme | null {
  try {
    const stored = localStorage.getItem(THEME_STORAGE_KEY);
    const theme = stored ? (JSON.parse(stored) as string) : null;

    return theme && isTheme(theme) ? theme : null;
  } catch {
    return null;
  }
}

function browserTheme(): Theme {
  const prefersDark = window.matchMedia?.(
    '(prefers-color-scheme: dark)'
  ).matches;

  return prefersDark ? 'dark' : 'light';
}
