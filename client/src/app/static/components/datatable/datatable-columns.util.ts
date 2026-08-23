import { DatatableColumn, DatatableColumnPreference } from './datatable.types';

const STORAGE_PREFIX = 'datatable:columns:';

function storageKey(key: string): string {
  return `${STORAGE_PREFIX}${key}`;
}

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
  key: string
): DatatableColumnPreference[] | null {
  try {
    const raw = localStorage.getItem(storageKey(key));

    if (!raw) return null;

    const parsed = JSON.parse(raw) as unknown;

    if (!Array.isArray(parsed)) return null;

    return parsed
      .filter(
        (entry): entry is DatatableColumnPreference =>
          typeof entry === 'object' &&
          entry !== null &&
          typeof (entry as DatatableColumnPreference).id === 'string' &&
          typeof (entry as DatatableColumnPreference).visible === 'boolean'
      )
      .map((entry) => ({ id: entry.id, visible: entry.visible }));
  } catch {
    return null;
  }
}

export function saveColumnPreferences(
  key: string,
  preferences: readonly DatatableColumnPreference[]
): void {
  try {
    localStorage.setItem(storageKey(key), JSON.stringify(preferences));
  } catch {
    // Ignore storage failures (private mode, quota, etc.).
  }
}
