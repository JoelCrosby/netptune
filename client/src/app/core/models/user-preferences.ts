import { ClientResponse } from './client-response';

export const COMMAND_PALETTE_RECENT_ITEMS_SCOPE =
  'commandPalette.recentItems.scope';
export const APPEARANCE_THEME = 'appearance.theme';

export type PreferenceScope = 'global' | 'workspace';

export interface PreferenceOption {
  value: string;
  label: string;
}

export interface PreferenceDefinition {
  key: string;
  groupKey: string;
  label: string;
  controlType: 'select';
  valueType: 'string';
  defaultValue: unknown;
  allowedScopes: PreferenceScope[];
  options: PreferenceOption[];
  order: number;
}

export interface PreferenceDefinitionGroup {
  key: string;
  label: string;
  order: number;
  preferences: PreferenceDefinition[];
}

export interface PreferenceDefinitionsResponse {
  groups: PreferenceDefinitionGroup[];
}

export interface ResolvedPreferenceValue {
  definition: PreferenceDefinition;
  globalValue: unknown | null;
  workspaceValue: unknown | null;
  effectiveValue: unknown;
  source: PreferenceScope | 'default';
}

export interface PreferenceValueGroup {
  key: string;
  label: string;
  order: number;
  preferences: ResolvedPreferenceValue[];
}

export interface PreferenceValuesResponse {
  groups: PreferenceValueGroup[];
}

export type PreferenceValueClientResponse =
  ClientResponse<ResolvedPreferenceValue>;
