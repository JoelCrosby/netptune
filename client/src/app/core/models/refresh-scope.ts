export const allRefreshScopes = [
  'tasks',
  'boardGroups',
  'boards',
  'sprints',
  'projects',
  'tags',
  'statuses',
  'relationTypes',
  'comments',
  'users',
  'notifications',
  'notificationSubscriptions',
  'profile',
  'pins',
  'pages',
  'sourceControl',
  'codeLinks',
  'activity',
  'automations',
] as const;

export type RefreshScope = (typeof allRefreshScopes)[number];
