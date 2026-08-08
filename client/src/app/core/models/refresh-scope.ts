export const allRefreshScopes = [
  'tasks',
  'boardGroups',
  'boards',
  'sprints',
  'projects',
  'tags',
  'statuses',
  'comments',
  'users',
  'notifications',
] as const;

export type RefreshScope = (typeof allRefreshScopes)[number];
