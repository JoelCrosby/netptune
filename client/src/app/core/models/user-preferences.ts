import { ClientResponse } from './client-response';

export const COMMAND_PALETTE_RECENT_ITEMS_SCOPE =
  'commandPalette.recentItems.scope';
export const APPEARANCE_THEME = 'appearance.theme';
export const BOARDS_HIDDEN_GROUP_IDS = 'boards.hiddenGroupIds';
export const BOARDS_TASK_SORT = 'boards.taskSort';
export const WORKSPACE_LAST_VISITED = 'workspace.lastVisited';

export type PreferenceScope = 'global' | 'workspace';

export interface PreferenceOption {
  value: string;
  label: string;
}

export interface PreferenceDefinition {
  key: string;
  groupKey: string;
  label: string;
  controlType: 'select' | 'hidden';
  valueType: 'string' | 'number-array' | 'number-array-map';
  defaultValue: unknown;
  allowedScopes: PreferenceScope[];
  options: PreferenceOption[];
  order: number;
  internal?: boolean;
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
