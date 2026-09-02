import { ClientResponse } from './client-response';

export const COMMAND_PALETTE_RECENT_ITEMS_SCOPE =
  'commandPalette.recentItems.scope';
export const APPEARANCE_THEME = 'appearance.theme';
export const APPEARANCE_TASK_DETAIL_LAYOUT = 'appearance.taskDetailLayout';
export const APPEARANCE_PAGE_WIDTH = 'appearance.pageWidth';
export const BOARDS_HIDDEN_GROUP_IDS = 'boards.hiddenGroupIds';
export const BOARDS_TASK_SORT = 'boards.taskSort';
export const VIEWS_PINNED_IDS = 'views.pinnedIds';
export const WORKSPACE_LAST_VISITED = 'workspace.lastVisited';
export const WORKSPACE_RECENT_IDS = 'workspace.recentIds';
export const WORKSPACES_PINNED_IDS = 'workspaces.pinnedIds';

export type PreferenceScope = 'global' | 'workspace';

export interface PreferenceOption {
  value: string;
  label: string;
}

export interface PreferenceDefinition {
  key: string;
  groupKey: string;
  label: string;
  controlType: 'select' | 'toggle' | 'hidden';
  valueType:
    'string' | 'boolean' | 'number-array' | 'number-array-map' | 'string-map';
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
