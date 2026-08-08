export type RefreshScope =
  | 'tasks'
  | 'boardGroups'
  | 'boards'
  | 'sprints'
  | 'projects'
  | 'tags'
  | 'statuses'
  | 'comments';

export const allRefreshScopes: readonly RefreshScope[] = [
  'tasks',
  'boardGroups',
  'boards',
  'sprints',
  'projects',
  'tags',
  'statuses',
  'comments',
];
