import {
  PreferenceScope,
  ResolvedPreferenceValue,
} from '@core/models/user-preferences';

export type PreferenceScopeSelection = Record<string, PreferenceScope>;

export function selectedScopeFor(
  preference: ResolvedPreferenceValue,
  selection: PreferenceScopeSelection
): PreferenceScope {
  const selected = selection[preference.definition.key];

  if (selected) return selected;
  if (preference.source === 'workspace') return 'workspace';

  const allowsGlobal = preference.definition.allowedScopes.includes('global');

  if (allowsGlobal) return 'global';

  return preference.definition.allowedScopes[0];
}

export function valueForScope(
  preference: ResolvedPreferenceValue,
  scope: PreferenceScope
): unknown {
  return scope === 'workspace'
    ? (preference.workspaceValue ?? preference.effectiveValue)
    : (preference.globalValue ?? preference.effectiveValue);
}

/** Ternaries in a template expression cannot be marked, so build the copy here. */
export function preferenceScopeLabel(scope: PreferenceScope): string {
  return scope === 'workspace'
    ? $localize`:Preference scope limited to the current workspace:Workspace`
    : $localize`:Preference scope applying everywhere:Global`;
}
