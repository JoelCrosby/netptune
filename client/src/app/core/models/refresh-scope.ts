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
  'profile',
  'pins',
] as const;

export type RefreshScope = (typeof allRefreshScopes)[number];
