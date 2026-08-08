import { allRefreshScopes, RefreshScope } from '@core/models/refresh-scope';

/** The views an entity kind invalidates. Keys match the entity types the server reports. */
const scopesByEntityType: Record<string, readonly RefreshScope[]> = {
  task: ['tasks', 'boardGroups', 'sprints'],
  sprint: ['sprints', 'tasks'],
  project: ['projects', 'boards', 'tasks'],
  board: ['boards', 'boardGroups'],
  tag: ['tags', 'tasks'],
  status: ['statuses', 'tasks', 'boardGroups'],
  comment: ['comments', 'tasks'],
};

/**
 * An entity kind the client has never heard of is worth an extra fetch — a view
 * left stale is the worse outcome.
 */
export const scopesForEntityType = (
  entityType: string
): readonly RefreshScope[] => {
  return scopesByEntityType[entityType] ?? allRefreshScopes;
};

export const refreshScopesForEntityTypes = (
  entityTypes: Iterable<string>
): Set<RefreshScope> => {
  const scopes = new Set<RefreshScope>();

  for (const entityType of entityTypes) {
    for (const scope of scopesForEntityType(entityType)) {
      scopes.add(scope);
    }
  }

  return scopes;
};
