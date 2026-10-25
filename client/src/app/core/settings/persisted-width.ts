import { inject, Signal, signal } from '@angular/core';
import {
  BrowserSettingKey,
  BrowserSettings,
  BrowserSettingsService,
} from './browser-settings.service';

type WidthSettingKey = {
  [K in BrowserSettingKey]: BrowserSettings[K] extends number ? K : never;
}[BrowserSettingKey];

export interface WidthBounds {
  min: number;
  max: number;
  fallback: number;
}

export interface PersistedWidth {
  readonly width: Signal<number>;
  setWidth(width: number): void;
  setResizing(resizing: boolean): void;
}

export function persistedWidth(
  key: WidthSettingKey,
  bounds: WidthBounds
): PersistedWidth {
  const settings = inject(BrowserSettingsService);
  const stored = settings.get(key);
  const width = signal(
    stored === undefined ? bounds.fallback : clampWidth(stored, bounds)
  );

  let resizing = false;

  const write = () => settings.set(key, width());

  return {
    width: width.asReadonly(),
    setWidth: (value) => {
      width.set(clampWidth(value, bounds));

      if (resizing) return;

      write();
    },
    setResizing: (value) => {
      resizing = value;

      if (resizing) return;

      write();
    },
  };
}

function clampWidth(width: number, bounds: WidthBounds): number {
  if (!Number.isFinite(width)) return bounds.fallback;

  const bounded = Math.min(bounds.max, Math.max(bounds.min, width));

  return Math.round(bounded);
}
