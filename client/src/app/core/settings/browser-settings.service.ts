import { Service, signal } from '@angular/core';
import { AiDisplayMode } from '@core/models/ai-display-mode';
import { AiEffort } from '@core/models/ai-effort';
import type { AiWorkspaceSession } from '@core/services/ai-session.service';
import type { Theme } from '@core/services/theme.service';
import { DatatableColumnPreference } from '@static/components/datatable/datatable.types';
import { DiffMode } from '@static/components/text-diff/text-diff';

export interface BrowserSettings {
  theme: Theme;
  'shell.sideNavExpanded': boolean;
  'pages.folderView': 'list' | 'grid';
  'pages.railCollapsed': boolean;
  'pages.sidebarWidth': number;
  'pages.treeExpanded': Record<string, number[]>;
  'ai.mode': AiDisplayMode;
  'ai.panelWidth': number;
  'ai.model': string | null;
  'ai.effort': AiEffort | null;
  'ai.appliedChangesExpanded': boolean;
  'ai.sessions': Record<string, AiWorkspaceSession>;
  'ai.drafts': Record<string, string>;
  'ai.review.mode': DiffMode;
  'ai.review.listWidth': number;
  'taskDetail.railWidth': number;
  'datatable.columns': Record<string, DatatableColumnPreference[]>;
}

export type BrowserSettingKey = keyof BrowserSettings;

const BROWSER_SETTINGS_KEY = 'Netptune-settings';

const clearedOnSignOut = new Set<BrowserSettingKey>([
  'ai.drafts',
  'ai.sessions',
  'pages.treeExpanded',
]);

type StoredSettings = Partial<BrowserSettings> & { owner?: string };

@Service()
export class BrowserSettingsService {
  private tabOwner: string | null = null;
  private readonly owner = signal<string | null>(readStored().owner ?? null);

  readonly storedOwner = this.owner.asReadonly();

  constructor() {
    window.addEventListener('storage', (event) => {
      const isSettingsChange =
        event.key === BROWSER_SETTINGS_KEY || event.key === null;

      if (!isSettingsChange) return;

      this.owner.set(readStored().owner ?? null);
    });
  }

  get<K extends BrowserSettingKey>(key: K): BrowserSettings[K] | undefined {
    const stored = readStored();

    if (!this.canAccess(stored, key)) return undefined;

    return valueOf(stored, key);
  }

  set<K extends BrowserSettingKey>(key: K, value: BrowserSettings[K]) {
    this.update(key, () => value);
  }

  update<K extends BrowserSettingKey>(
    key: K,
    fn: (current: BrowserSettings[K] | undefined) => BrowserSettings[K]
  ) {
    const stored = readStored();

    if (!this.canAccess(stored, key)) return;

    writeStored({ ...stored, [key]: fn(valueOf(stored, key)) });
  }

  remove(key: BrowserSettingKey) {
    const stored = readStored();

    if (!this.canAccess(stored, key)) return;

    const remaining = Object.entries(stored).filter(([name]) => {
      return name !== key;
    });

    writeStored(Object.fromEntries(remaining) as StoredSettings);
  }

  claim(userId: string) {
    this.tabOwner = userId;

    const stored = readStored();

    if (stored.owner !== userId) {
      writeStored({ ...withoutUserSettings(stored), owner: userId });
    }

    this.owner.set(userId);
  }

  release() {
    this.tabOwner = null;

    const next = withoutUserSettings(readStored());

    delete next.owner;
    writeStored(next);
    this.owner.set(null);
  }

  private canAccess(stored: StoredSettings, key: BrowserSettingKey): boolean {
    if (!clearedOnSignOut.has(key)) return true;

    return this.tabOwner !== null && stored.owner === this.tabOwner;
  }
}

function valueOf<K extends BrowserSettingKey>(
  stored: StoredSettings,
  key: K
): BrowserSettings[K] | undefined {
  return stored[key] as BrowserSettings[K] | undefined;
}

function withoutUserSettings(stored: StoredSettings): StoredSettings {
  const entries = Object.entries(stored).filter(([key]) => {
    return !clearedOnSignOut.has(key as BrowserSettingKey);
  });

  return Object.fromEntries(entries) as StoredSettings;
}

function readStored(): StoredSettings {
  try {
    const raw = localStorage.getItem(BROWSER_SETTINGS_KEY);
    const parsed: unknown = raw ? JSON.parse(raw) : {};
    const isObject =
      typeof parsed === 'object' && parsed !== null && !Array.isArray(parsed);

    return isObject ? (parsed as StoredSettings) : {};
  } catch {
    return {};
  }
}

function writeStored(settings: StoredSettings) {
  try {
    localStorage.setItem(BROWSER_SETTINGS_KEY, JSON.stringify(settings));
  } catch {
    return;
  }
}
