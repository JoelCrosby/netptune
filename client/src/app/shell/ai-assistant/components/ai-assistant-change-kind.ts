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
  propose_create_page: 'create',
  propose_create_automation: 'create',
  propose_delete_task: 'delete',
  propose_delete_sprint: 'delete',
  propose_delete_board: 'delete',
  propose_delete_board_group: 'delete',
  propose_remove_task_from_sprint: 'delete',
  propose_remove_task_from_board: 'delete',
  propose_archive_page: 'delete',
  propose_delete_page: 'delete',
  propose_delete_tag: 'delete',
  propose_delete_status: 'delete',
  propose_delete_project: 'delete',
  propose_delete_relation_type: 'delete',
  propose_delete_comment: 'delete',
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
const ARCHIVE = $localize`:verb|What a proposed change does:Archive`;
const RESTORE = $localize`:What a proposed change does:Restore`;

const ACTIONS: Record<string, string> = {
  propose_create_task: CREATE,
  propose_create_project: CREATE,
  propose_create_sprint: CREATE,
  propose_create_board: CREATE,
  propose_create_board_group: CREATE,
  propose_create_status: CREATE,
  propose_create_tag: CREATE,
  propose_create_relation_type: CREATE,
  propose_create_page: CREATE,
  propose_create_automation: CREATE,
  propose_update_task: UPDATE,
  propose_update_automation: UPDATE,
  propose_set_automation_enabled: UPDATE,
  propose_update_project: UPDATE,
  propose_update_board: UPDATE,
  propose_update_board_group: UPDATE,
  propose_update_status: UPDATE,
  propose_update_tag: UPDATE,
  propose_update_relation_type: UPDATE,
  propose_update_comment: UPDATE,
  propose_update_sprint: UPDATE,
  propose_start_sprint: UPDATE,
  propose_complete_sprint: UPDATE,
  propose_cancel_sprint: UPDATE,
  propose_resolve_task_flag: UPDATE,
  propose_update_page_body: UPDATE,
  propose_update_page_details: UPDATE,
  propose_restore_page: RESTORE,
  propose_restore_page_version: RESTORE,
  propose_delete_task: DELETE,
  propose_delete_sprint: DELETE,
  propose_delete_board: DELETE,
  propose_delete_board_group: DELETE,
  propose_delete_page: DELETE,
  propose_delete_tag: DELETE,
  propose_delete_status: DELETE,
  propose_delete_project: DELETE,
  propose_delete_relation_type: DELETE,
  propose_delete_comment: DELETE,
  propose_archive_page: ARCHIVE,
  propose_assign_task: ASSIGN,
  propose_set_task_tags: TAG,
  propose_add_comment: COMMENT,
  propose_link_tasks: LINK,
  propose_unlink_tasks: UNLINK,
  propose_move_task_to_sprint: MOVE,
  propose_move_task_to_board_group: MOVE,
  propose_reorder_board_groups: MOVE,
  propose_add_tasks_to_sprint: MOVE,
  propose_remove_task_from_sprint: MOVE,
  propose_remove_task_from_board: MOVE,
  propose_move_page: MOVE,
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
const DANGLING_WORDS: Record<string, string> = {
  en: 'on in into from to of for with',
  fr: 'sur dans de du des à au aux vers en pour avec depuis',
  de: 'auf in im von vom zu zum zur nach für mit aus an am',
  es: 'en de del a al para con sobre desde hacia',
};

const DANGLING = new RegExp(
  `\\s+(${Object.values(DANGLING_WORDS).join(' ').replaceAll(' ', '|')})$`,
  'i'
);

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
