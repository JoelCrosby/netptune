import { BrowserSettingsService } from '@core/settings/browser-settings.service';
import { DatatableColumn, DatatableColumnPreference } from './datatable.types';

export interface ReconcileColumnOptions {
  newColumnsVisible?: boolean;
}

export function reconcileColumnPreferences<T>(
  columns: readonly DatatableColumn<T>[],
  preferences: readonly DatatableColumnPreference[] | null,
  options: ReconcileColumnOptions = {}
): DatatableColumnPreference[] {
  const { newColumnsVisible = true } = options;
  const byId = new Map(columns.map((column) => [column.id, column]));
  const seen = new Set<string>();
  const result: DatatableColumnPreference[] = [];

  for (const preference of preferences ?? []) {
    if (byId.has(preference.id) && !seen.has(preference.id)) {
      result.push({ id: preference.id, visible: preference.visible });
      seen.add(preference.id);
    }
  }

  for (const column of columns) {
    if (!seen.has(column.id)) {
      result.push({ id: column.id, visible: newColumnsVisible });
    }
  }

  return result;
}

export function loadColumnPreferences(
  settings: BrowserSettingsService,
  key: string
): DatatableColumnPreference[] | null {
  const stored: unknown = settings.get('datatable.columns')?.[key];

  if (!Array.isArray(stored)) return null;

  return stored
    .filter(
      (entry): entry is DatatableColumnPreference =>
        typeof entry === 'object' &&
        entry !== null &&
        typeof (entry as DatatableColumnPreference).id === 'string' &&
        typeof (entry as DatatableColumnPreference).visible === 'boolean'
    )
    .map((entry) => ({ id: entry.id, visible: entry.visible }));
}

export function saveColumnPreferences(
  settings: BrowserSettingsService,
  key: string,
  preferences: readonly DatatableColumnPreference[]
): void {
  settings.update('datatable.columns', (byTable) => {
    return { ...byTable, [key]: [...preferences] };
  });
}
