import { EstimateType } from '@core/enums/estimate-type';
import { TaskPriority } from '@core/enums/task-priority';
import {
  AiChangeEditorKind,
  AiChangeField,
  AiChangeFieldEditor,
  AiProposedChange,
} from '@core/models/ai-conversation';
import { AiChangeFieldEdit } from '@core/services/ai-api.service';
import { findPriority, parseChangeEstimate } from './ai-assistant-field-diff';

export interface AiFieldDraft {
  value: string | null;
  values: string[];
  number: number | null;
}

// The server reads these as C# enum names, not as the labels shown to the reviewer.
export const PRIORITY_NAMES: Record<TaskPriority, string> = {
  [TaskPriority.none]: 'None',
  [TaskPriority.low]: 'Low',
  [TaskPriority.medium]: 'Medium',
  [TaskPriority.high]: 'High',
  [TaskPriority.critical]: 'Critical',
};

export const ESTIMATE_TYPE_NAMES: Record<EstimateType, string> = {
  [EstimateType.storyPoints]: 'StoryPoints',
  [EstimateType.hours]: 'Hours',
  [EstimateType.tShirt]: 'TShirt',
};

const TEXT_KINDS = new Set([
  AiChangeEditorKind.text,
  AiChangeEditorKind.markdown,
]);

const LIST_KINDS = new Set([AiChangeEditorKind.users, AiChangeEditorKind.tags]);

const toDraft = (draft: Partial<AiFieldDraft>): AiFieldDraft => {
  return { value: null, values: [], number: null, ...draft };
};

const initialPriority = (field: AiChangeField): AiFieldDraft => {
  const priority = findPriority(field.after ?? '');

  return toDraft({
    value: priority === null ? null : PRIORITY_NAMES[priority],
  });
};

const initialEstimate = (field: AiChangeField): AiFieldDraft => {
  const estimate = parseChangeEstimate(field.after);

  if (!estimate) {
    return toDraft({});
  }

  return toDraft({
    value: ESTIMATE_TYPE_NAMES[estimate.type],
    number: estimate.value,
  });
};

export const initialDraft = (field: AiChangeField): AiFieldDraft => {
  const kind = field.editor?.kind;
  const values = field.afterValues ?? [];

  if (kind === undefined || TEXT_KINDS.has(kind)) {
    return toDraft({ value: field.after ?? '' });
  }

  switch (kind) {
    case AiChangeEditorKind.priority:
      return initialPriority(field);
    case AiChangeEditorKind.estimate:
      return initialEstimate(field);
    case AiChangeEditorKind.date:
      return toDraft({ value: values[0]?.display ?? null });
    case AiChangeEditorKind.users:
      return toDraft({ values: values.flatMap((value) => value.id ?? []) });
    case AiChangeEditorKind.tags:
      return toDraft({ values: values.map((value) => value.display) });
    default:
      return toDraft({ value: values[0]?.id ?? null });
  }
};

export const isSameDraft = (left: AiFieldDraft, right: AiFieldDraft) => {
  return JSON.stringify(left) === JSON.stringify(right);
};

export const isDraftComplete = (
  editor: AiChangeFieldEditor,
  draft: AiFieldDraft
): boolean => {
  if (TEXT_KINDS.has(editor.kind)) {
    return (draft.value ?? '').trim().length > 0;
  }

  if (LIST_KINDS.has(editor.kind)) {
    return true;
  }

  if (draft.value === null) {
    return editor.isClearable;
  }

  const isEstimate = editor.kind === AiChangeEditorKind.estimate;

  return !isEstimate || draft.number !== null;
};

export const toFieldEdit = (
  name: string,
  editor: AiChangeFieldEditor,
  draft: AiFieldDraft
): AiChangeFieldEdit => {
  if (LIST_KINDS.has(editor.kind)) {
    return { name, values: draft.values };
  }

  if (editor.kind === AiChangeEditorKind.estimate) {
    return { name, value: draft.value, number: draft.number };
  }

  return { name, value: draft.value };
};

const CREATE_TAG_CHANGE = 'propose_create_tag';

export const proposedTagNames = (changes: AiProposedChange[]): string[] => {
  return changes
    .filter((change) => change.toolName === CREATE_TAG_CHANGE)
    .flatMap((change) => change.fields)
    .flatMap((field) => field.afterValues ?? [])
    .map((value) => value.display);
};
