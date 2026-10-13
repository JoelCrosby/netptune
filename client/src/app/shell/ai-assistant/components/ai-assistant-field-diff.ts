import {
  AiChangeField,
  AiChangeValue,
  AiChangeValueKind,
} from '@core/models/ai-conversation';
import { EstimateType, formatEstimate } from '@core/enums/estimate-type';
import { TaskPriority, taskPriorityLabels } from '@core/enums/task-priority';
import { DiffOp, diffSequence } from '@static/components/text-diff/text-diff';
import { ValueChipAccent } from '@static/components/value-chip/value-chip.component';
import { marked } from 'marked';
import { afterValues, beforeValues } from './ai-assistant-change-kind';

export type AiFieldDiffKind =
  'text' | 'markdown' | 'choice' | 'date' | 'set' | 'estimate' | 'relation';

const MARKDOWN_FIELDS = new Set(['description', 'body', 'comment']);

// Priority and board group arrive as text; the server renders them before they reach a field.
const CHOICE_TEXT_FIELDS = new Set(['priority', 'boardGroup']);

const ISO_DATE = /^(\d{4})-(\d{2})-(\d{2})/;

const textDiffKind = (field: AiChangeField): AiFieldDiffKind => {
  if (MARKDOWN_FIELDS.has(field.name)) {
    return 'markdown';
  }

  if (CHOICE_TEXT_FIELDS.has(field.name)) {
    return 'choice';
  }

  if (field.name === 'estimate') {
    return 'estimate';
  }

  const isDateNamed = field.name.endsWith('Date');
  const isDateShaped = [field.before, field.after].every((value) => {
    return !value || ISO_DATE.test(value);
  });

  return isDateNamed && isDateShaped ? 'date' : 'text';
};

export const fieldDiffKind = (field: AiChangeField): AiFieldDiffKind => {
  switch (field.kind) {
    case AiChangeValueKind.date:
      return 'date';
    case AiChangeValueKind.status:
    case AiChangeValueKind.sprint:
      return 'choice';
    case AiChangeValueKind.user:
    case AiChangeValueKind.tag:
      return 'set';
    case AiChangeValueKind.task:
      return 'relation';
    default:
      return textDiffKind(field);
  }
};

export interface AiChoice {
  label: string;
  color: string | null;
  accent: ValueChipAccent;
}

const PRIORITY_ACCENTS: Record<TaskPriority, ValueChipAccent> = {
  [TaskPriority.none]: 'grey',
  [TaskPriority.low]: 'blue',
  [TaskPriority.medium]: 'yellow',
  [TaskPriority.high]: 'orange',
  [TaskPriority.critical]: 'red',
};

export const findPriority = (text: string): TaskPriority | null => {
  const normalised = text.trim().toLowerCase();
  const match = Object.entries(taskPriorityLabels).find(([, label]) => {
    return label.toLowerCase() === normalised;
  });

  return match ? (Number(match[0]) as TaskPriority) : null;
};

const toChoice = (name: string, value: AiChangeValue): AiChoice => {
  if (name !== 'priority') {
    return { label: value.display, color: value.color ?? null, accent: 'none' };
  }

  const priority = findPriority(value.display);

  if (priority === null) {
    return { label: value.display, color: null, accent: 'none' };
  }

  return {
    label: taskPriorityLabels[priority],
    color: null,
    accent: PRIORITY_ACCENTS[priority],
  };
};

export const choiceBefore = (field: AiChangeField): AiChoice | null => {
  const [value] = beforeValues(field);

  return value ? toChoice(field.name, value) : null;
};

export const choiceAfter = (field: AiChangeField): AiChoice | null => {
  const [value] = afterValues(field);

  return value ? toChoice(field.name, value) : null;
};

const firstDisplay = (
  values: AiChangeValue[] | null | undefined,
  rendered: string | null | undefined
): string | null => {
  return values?.[0]?.display ?? rendered ?? null;
};

// A bare yyyy-MM-dd read through Date() lands at UTC midnight, which is the day before west of it.
export const parseChangeDate = (value: string | null): Date | null => {
  const match = value ? ISO_DATE.exec(value) : null;

  if (!match) {
    return null;
  }

  const [, year, month, day] = match.map(Number);

  return new Date(year, month - 1, day);
};

export const dateBefore = (field: AiChangeField): Date | null => {
  return parseChangeDate(firstDisplay(field.beforeValues, field.before));
};

export const dateAfter = (field: AiChangeField): Date | null => {
  return parseChangeDate(firstDisplay(field.afterValues, field.after));
};

const DAY_MS = 24 * 60 * 60 * 1000;

export const dayShift = (before: Date | null, after: Date | null): number => {
  if (!before || !after) {
    return 0;
  }

  return Math.round((after.getTime() - before.getTime()) / DAY_MS);
};

const ESTIMATE_TYPES: Record<string, EstimateType> = {
  storypoints: EstimateType.storyPoints,
  hours: EstimateType.hours,
  tshirt: EstimateType.tShirt,
};

interface AiEstimateUnit {
  one: string;
  many: string;
}

const ESTIMATE_UNITS: Partial<Record<EstimateType, AiEstimateUnit>> = {
  [EstimateType.storyPoints]: {
    one: $localize`:Unit of a single story point estimate:Story Point`,
    many: $localize`:Unit of an estimate of several story points:Story Points`,
  },
  [EstimateType.hours]: {
    one: $localize`:Unit of a single hour estimate:Hour`,
    many: $localize`:Unit of an estimate of several hours:Hours`,
  },
};

const ESTIMATE_TEXT = /^\s*(\d+(?:\.\d+)?)\s+(.+?)\s*$/;

export interface AiChangeEstimate {
  type: EstimateType;
  value: number;
}

// The server writes an estimate as "<value> <EstimateType name>", e.g. "3 StoryPoints".
export const parseChangeEstimate = (
  text: string | null | undefined
): AiChangeEstimate | null => {
  const match = text ? ESTIMATE_TEXT.exec(text) : null;

  if (!match) {
    return null;
  }

  const type = ESTIMATE_TYPES[match[2].toLowerCase().replace(/[^a-z]/g, '')];

  return type === undefined ? null : { type, value: Number(match[1]) };
};

export const formatChangeEstimate = (
  text: string | null | undefined
): string | null => {
  const estimate = parseChangeEstimate(text);

  if (!estimate) {
    return text ?? null;
  }

  const { type, value } = estimate;
  const unit = ESTIMATE_UNITS[type];

  if (!unit) {
    return formatEstimate(type, value);
  }

  return `${value} ${value === 1 ? unit.one : unit.many}`;
};

export interface AiRelatedTask {
  key: string;
  systemId: string | null;
  name: string;
}

// Task values read "<system id> · <name>"; a task proposed in the same set has no system id yet.
const SYSTEM_ID_DISPLAY = /^(\S+-\d+) · (.+)$/s;

export const toRelatedTask = (value: AiChangeValue): AiRelatedTask => {
  const match = SYSTEM_ID_DISPLAY.exec(value.display);
  const key = value.id ?? value.display;

  if (!match) {
    return { key, systemId: null, name: value.display };
  }

  return { key, systemId: match[1], name: match[2] };
};

const markdownBlocks = (text: string | null | undefined): string[] => {
  return marked
    .lexer(text ?? '', { gfm: true })
    .filter((token) => token.type !== 'space')
    .map((token) => token.raw.trim())
    .filter((raw) => raw.length > 0);
};

export const markdownDiff = (
  before: string | null | undefined,
  after: string | null | undefined
): DiffOp[] => diffSequence(markdownBlocks(before), markdownBlocks(after));
