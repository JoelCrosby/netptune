export const allRefreshScopes = [
  'tasks',
  'boardGroups',
  'boards',
  'sprints',
  'projects',
  'tags',
  'statuses',
  'comments',
] as const;

export type RefreshScope = (typeof allRefreshScopes)[number];
