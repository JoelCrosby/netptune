import { effect, inject, Service, signal } from '@angular/core';
import { BrowserSettingsService } from '@core/settings/browser-settings.service';

export type Theme = 'light' | 'dark';

const themes = new Set<string>(['light', 'dark']);

function isTheme(value: string): value is Theme {
  return themes.has(value);
}

@Service()
export class ThemeService {
  private readonly settings = inject(BrowserSettingsService);
  private readonly current = signal(this.initialTheme());

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
    this.settings.set('theme', chosen);
  }

  clear() {
    this.settings.remove('theme');
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

  private initialTheme(): Theme {
    const stored = this.settings.get('theme');
    const isKnown = typeof stored === 'string' && isTheme(stored);

    return isKnown ? stored : browserTheme();
  }
}

function browserTheme(): Theme {
  const prefersDark = window.matchMedia?.(
    '(prefers-color-scheme: dark)'
  ).matches;

  return prefersDark ? 'dark' : 'light';
}
