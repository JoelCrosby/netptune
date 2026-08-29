import {
  AiChangeField,
  AiChangeValue,
  AiProposedChange,
} from '@core/models/ai-conversation';
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
  propose_create_relation_type: 'create',
  propose_delete_task: 'delete',
  propose_delete_sprint: 'delete',
  propose_remove_task_from_sprint: 'delete',
  propose_assign_task: 'collection',
  propose_set_task_tags: 'collection',
  propose_add_tasks_to_sprint: 'collection',
  propose_link_tasks: 'collection',
  propose_unlink_tasks: 'collection',
  propose_add_comment: 'comment',
};

const CREATE = $localize`:What a proposed change does:Create`;
const UPDATE = $localize`:What a proposed change does:Update`;
const DELETE = $localize`:What a proposed change does:Delete`;
const ASSIGN = $localize`:What a proposed change does:Assign`;
const TAG = $localize`:What a proposed change does:Tag`;
const COMMENT = $localize`:What a proposed change does:Comment`;
const LINK = $localize`:What a proposed change does:Link`;
const UNLINK = $localize`:What a proposed change does:Unlink`;
const MOVE = $localize`:What a proposed change does:Move`;

const ACTIONS: Record<string, string> = {
  propose_create_task: CREATE,
  propose_create_project: CREATE,
  propose_create_sprint: CREATE,
  propose_create_board: CREATE,
  propose_create_board_group: CREATE,
  propose_create_status: CREATE,
  propose_create_tag: CREATE,
  propose_create_relation_type: CREATE,
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
  propose_unlink_tasks: UNLINK,
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
const DANGLING = /\s+(on|in|into|from|to|of|for|with)$/i;

const TARGET_QUOTES: Record<string, number> = {
  propose_resolve_task_flag: 1,
};

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
  kept: AiChangeValue[];
  added: AiChangeValue[];
  removed: AiChangeValue[];
}

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

const valueKey = (value: AiChangeValue): string => value.id ?? value.display;

const readValues = (
  values: AiChangeValue[] | null | undefined,
  rendered: string | null | undefined
): AiChangeValue[] => {
  if (values) {
    return values;
  }

  return splitValues(rendered).map((display) => ({ display }));
};

export const beforeValues = (field: AiChangeField): AiChangeValue[] => {
  return readValues(field.beforeValues, field.before);
};

export const afterValues = (field: AiChangeField): AiChangeValue[] => {
  return readValues(field.afterValues, field.after);
};

export const valueDiff = (field: AiChangeField): AiValueDiff => {
  const before = readValues(field.beforeValues, field.before);
  const after = readValues(field.afterValues, field.after);
  const beforeKeys = new Set(before.map(valueKey));
  const afterKeys = new Set(after.map(valueKey));

  return {
    kept: after.filter((value) => beforeKeys.has(valueKey(value))),
    added: after.filter((value) => !beforeKeys.has(valueKey(value))),
    removed: before.filter((value) => !afterKeys.has(valueKey(value))),
  };
};
