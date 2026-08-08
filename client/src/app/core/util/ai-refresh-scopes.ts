import {
  AiChangeApplyStatus,
  AiProposedChange,
} from '@core/models/ai-conversation';
import { allRefreshScopes, RefreshScope } from '@core/models/refresh-scope';

const scopesByEntityType: Record<string, RefreshScope[]> = {
  task: ['tasks', 'boardGroups', 'sprints'],
  sprint: ['sprints', 'tasks'],
  project: ['projects', 'boards', 'tasks'],
  board: ['boards', 'boardGroups'],
  tag: ['tags', 'tasks'],
  status: ['statuses', 'tasks', 'boardGroups'],
};

const scopesByToolName: Record<string, RefreshScope[]> = {
  propose_add_comment: ['comments', 'tasks'],
  propose_set_task_tags: ['tasks', 'boardGroups', 'sprints', 'tags'],
};

/**
 * A change the server refuses to describe is worth an extra fetch — a view left
 * stale after a tool the client has never heard of is the worse outcome.
 */
const scopesForChange = (change: AiProposedChange): readonly RefreshScope[] => {
  return (
    scopesByToolName[change.toolName] ??
    scopesByEntityType[change.entityType] ??
    allRefreshScopes
  );
};

export const refreshScopesForChanges = (
  changes: readonly AiProposedChange[]
): Set<RefreshScope> => {
  const scopes = new Set<RefreshScope>();

  for (const change of changes) {
    for (const scope of scopesForChange(change)) {
      scopes.add(scope);
    }
  }

  return scopes;
};

/** Changes that touched workspace data in the exchange that just completed. */
export const landedChanges = (
  before: readonly AiProposedChange[],
  after: readonly AiProposedChange[]
): AiProposedChange[] => {
  const previous = new Map(before.map((change) => [change.id, change]));

  return after.filter((change) => {
    const priorState = previous.get(change.id);
    const wasApplied = priorState?.applyStatus === AiChangeApplyStatus.applied;
    const isApplied = change.applyStatus === AiChangeApplyStatus.applied;
    const isUndone = !!change.undoneAt && !priorState?.undoneAt;

    return (isApplied && !wasApplied) || isUndone;
  });
};
