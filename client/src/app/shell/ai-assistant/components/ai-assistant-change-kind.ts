import { AiChangeField, AiProposedChange } from '@core/models/ai-conversation';
import { BadgeColor } from '@static/components/badge/badge.component';

export type AiChangeKind =
  'create' | 'update' | 'delete' | 'collection' | 'comment';

const KINDS: Record<string, AiChangeKind> = {
  propose_create_task: 'create',
  propose_create_project: 'create',
  propose_create_sprint: 'create',
  propose_create_board: 'create',
  propose_create_board_group: 'create',
  propose_create_status: 'create',
  propose_create_tag: 'create',
  propose_delete_task: 'delete',
  propose_delete_sprint: 'delete',
  propose_remove_task_from_sprint: 'delete',
  propose_assign_task: 'collection',
  propose_set_task_tags: 'collection',
  propose_add_tasks_to_sprint: 'collection',
  propose_link_tasks: 'collection',
  propose_add_comment: 'comment',
};

const CREATE = $localize`:What a proposed change does:Create`;
const UPDATE = $localize`:What a proposed change does:Update`;
const DELETE = $localize`:What a proposed change does:Delete`;
const ASSIGN = $localize`:What a proposed change does:Assign`;
const TAG = $localize`:What a proposed change does:Tag`;
const COMMENT = $localize`:What a proposed change does:Comment`;
const LINK = $localize`:What a proposed change does:Link`;
const MOVE = $localize`:What a proposed change does:Move`;

const ACTIONS: Record<string, string> = {
  propose_create_task: CREATE,
  propose_create_project: CREATE,
  propose_create_sprint: CREATE,
  propose_create_board: CREATE,
  propose_create_board_group: CREATE,
  propose_create_status: CREATE,
  propose_create_tag: CREATE,
  propose_update_task: UPDATE,
  propose_update_project: UPDATE,
  propose_update_sprint: UPDATE,
  propose_start_sprint: UPDATE,
  propose_complete_sprint: UPDATE,
  propose_cancel_sprint: UPDATE,
  propose_resolve_task_flag: UPDATE,
  propose_delete_task: DELETE,
  propose_delete_sprint: DELETE,
  propose_assign_task: ASSIGN,
  propose_set_task_tags: TAG,
  propose_add_comment: COMMENT,
  propose_link_tasks: LINK,
  propose_move_task_to_sprint: MOVE,
  propose_move_task_to_board_group: MOVE,
  propose_add_tasks_to_sprint: MOVE,
  propose_remove_task_from_sprint: MOVE,
};

const KIND_ACTIONS: Record<AiChangeKind, string> = {
  create: CREATE,
  update: UPDATE,
  delete: DELETE,
  collection: UPDATE,
  comment: COMMENT,
};

const TONES: Record<AiChangeKind, BadgeColor> = {
  create: 'success',
  update: 'info',
  delete: 'warn',
  collection: 'primary',
  comment: 'neutral',
};

export const changeKind = (change: AiProposedChange): AiChangeKind => {
  return KINDS[change.toolName] ?? 'update';
};

export const changeAction = (change: AiProposedChange): string => {
  return ACTIONS[change.toolName] ?? KIND_ACTIONS[changeKind(change)];
};

export const changeTone = (change: AiProposedChange): BadgeColor => {
  return TONES[changeKind(change)];
};

export interface AiChangeSummary {
  target: string | null;
  detail: string;
}

const QUOTED = /“([^”]+)”/g;

/** Lifting the name out leaves the preposition that introduced it dangling. */
const DANGLING = /\s+(on|in|into|from|to|of|for|with)$/i;

/** The name is quoted second only where the tool leads with something else. */
const TARGET_QUOTES: Record<string, number> = {
  propose_resolve_task_flag: 1,
};

/**
 * The tools write one sentence naming the entity in quotes. The name belongs in
 * its own column, so it is lifted out and the rest is left as the phrase.
 */
export const changeSummary = (change: AiProposedChange): AiChangeSummary => {
  const summary = change.summary;
  const quotes = [...summary.matchAll(QUOTED)];
  const quote = quotes[TARGET_QUOTES[change.toolName] ?? 0];

  if (!quote) {
    return { target: null, detail: summary };
  }

  const head = summary.slice(0, quote.index).trimEnd().replace(DANGLING, '');
  const tail = summary.slice(quote.index + quote[0].length);

  return {
    target: quote[1],
    detail: `${head} ${tail}`.replace(/\s+/g, ' ').trim(),
  };
};

export interface AiValueDiff {
  kept: string[];
  added: string[];
  removed: string[];
}

/** The tools write an empty collection as this literal rather than leaving it out. */
const EMPTY_COLLECTION = 'none';

export const splitValues = (value: string | null | undefined): string[] => {
  const trimmed = value?.trim() ?? '';
  const isEmpty =
    trimmed.length === 0 || trimmed.toLowerCase() === EMPTY_COLLECTION;

  if (isEmpty) {
    return [];
  }

  return trimmed
    .split(',')
    .map((part) => part.trim())
    .filter((part) => part.length > 0);
};

export const valueDiff = (field: AiChangeField): AiValueDiff => {
  const before = splitValues(field.before);
  const after = splitValues(field.after);

  return {
    kept: after.filter((value) => before.includes(value)),
    added: after.filter((value) => !before.includes(value)),
    removed: before.filter((value) => !after.includes(value)),
  };
};
