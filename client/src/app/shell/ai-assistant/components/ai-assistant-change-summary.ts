import {
  AiChangeApplyStatus,
  AiChangeField,
  AiProposedChange,
} from '@core/models/ai-conversation';
import {
  changeRoute,
  entityLabel,
  fieldLabel,
  isProseField,
  isValid,
} from './ai-assistant-change-group';
import { changeSummary } from './ai-assistant-change-kind';
import { AiChangeLetter, changeLetter, lineOps } from './ai-assistant-diff';

/**
 * One line per kind of change rather than per change, so a large set is
 * described completely instead of being truncated.
 */
export interface AiDigestRow {
  key: string;
  letter: AiChangeLetter | null;
  lead: string;
  emphasis: string;
  trail: string;
  label: string;
  scope: string | null;
  changeIds: number[];
}

export interface AiDigestRowView extends AiDigestRow {
  isIncluded: boolean;
}

export interface AiInlineValue {
  mark: string | null;
  isAdded: boolean;
  text: string;
}

export interface AiInlineSwap {
  before: string;
  after: string;
}

export interface AiInlineField {
  key: string;
  label: string;
  isProse: boolean;
  lines: AiInlineValue[];
  swap: AiInlineSwap | null;
  single: AiInlineValue | null;
}

export interface AiInlineRow {
  change: AiProposedChange;
  letter: AiChangeLetter;
  isSelectable: boolean;
  isIncluded: boolean;
  fields: AiInlineField[];
}

export interface AiInlineGroup {
  key: string;
  heading: string;
  rows: AiInlineRow[];
}

interface AiDigestGroup {
  key: string;
  letter: AiChangeLetter;
  entityType: string;
  fieldName: string;
  changes: AiProposedChange[];
}

/** Five rows keep the block under a third of the panel, whatever the set size. */
const DIGEST_ROW_LIMIT = 5;

const LETTER_ORDER: Record<AiChangeLetter, number> = { M: 0, A: 1, D: 2 };

const ENTITY_PLURALS: Record<string, string> = {
  task: $localize`:Plural of the entity a proposed change targets:Tasks`,
  project: $localize`:Plural of the entity a proposed change targets:Projects`,
  sprint: $localize`:Plural of the entity a proposed change targets:Sprints`,
  board: $localize`:Plural of the entity a proposed change targets:Boards`,
  boardGroup: $localize`:Plural of the entity a proposed change targets:Board groups`,
  status: $localize`:Plural of the entity a proposed change targets:Statuses`,
  comment: $localize`:Plural of the entity a proposed change targets:Comments`,
  tag: $localize`:Plural of the entity a proposed change targets:Tags`,
  relationType: $localize`:Plural of the entity a proposed change targets:Relation types`,
};

/**
 * The digest names what happened to a field across many entities, which reads
 * as a phrase — "Tags set on 32 tasks" — rather than as the field alone.
 */
const FIELD_PHRASES: Record<string, string> = {
  name: $localize`:Digest phrase for renamed entities:Names changed`,
  description: $localize`:Digest phrase for rewritten descriptions:Descriptions rewritten`,
  status: $localize`:Digest phrase for moved entities:Statuses changed`,
  priority: $localize`:Digest phrase for re-prioritised entities:Priorities changed`,
  estimate: $localize`:Digest phrase for re-estimated entities:Estimates changed`,
  startDate: $localize`:Digest phrase for moved start dates:Start dates changed`,
  endDate: $localize`:Digest phrase for moved end dates:End dates changed`,
  dueDate: $localize`:Digest phrase for moved due dates:Due dates changed`,
  assignees: $localize`:Digest phrase for reassigned entities:Assignees changed`,
  tags: $localize`:Digest phrase for retagged entities:Tags set`,
  sprint: $localize`:Digest phrase for entities moved between sprints:Sprints changed`,
  project: $localize`:Digest phrase for entities moved between projects:Projects changed`,
  board: $localize`:Digest phrase for entities moved between boards:Boards changed`,
  boardGroup: $localize`:Digest phrase for entities moved between board groups:Board groups changed`,
  goal: $localize`:Digest phrase for rewritten goals:Goals rewritten`,
  comment: $localize`:Digest phrase for added comments:Comments added`,
  flag: $localize`:Digest phrase for resolved flags:Flags resolved`,
};

/**
 * German capitalises its nouns wherever they sit, so the counted form is its
 * own message rather than a lower-cased copy of the one that opens a row.
 */
const ENTITY_COUNTS: Record<string, string> = {
  task: $localize`:Plural of the entity a digest row counts, mid sentence:tasks`,
  project: $localize`:Plural of the entity a digest row counts, mid sentence:projects`,
  sprint: $localize`:Plural of the entity a digest row counts, mid sentence:sprints`,
  board: $localize`:Plural of the entity a digest row counts, mid sentence:boards`,
  boardGroup: $localize`:Plural of the entity a digest row counts, mid sentence:board groups`,
  status: $localize`:Plural of the entity a digest row counts, mid sentence:statuses`,
  comment: $localize`:Plural of the entity a digest row counts, mid sentence:comments`,
  tag: $localize`:Plural of the entity a digest row counts, mid sentence:tags`,
  relationType: $localize`:Plural of the entity a digest row counts, mid sentence:relation types`,
};

/** The only fields that name a place every change in a row could share. */
const SCOPE_FIELDS = ['project', 'board'];

const entityPlural = (entityType: string): string => {
  return ENTITY_PLURALS[entityType] ?? entityLabel(entityType);
};

const countedEntities = (entityType: string): string => {
  return ENTITY_COUNTS[entityType] ?? entityLabel(entityType);
};

const fieldPhrase = (name: string): string => {
  const known = FIELD_PHRASES[name];

  if (known) {
    return known;
  }

  const label = fieldLabel(name);

  return $localize`:Digest phrase for a field with no phrasing of its own:${label}:FIELD: changed`;
};

const primaryField = (change: AiProposedChange): AiChangeField | null => {
  return change.fields[0] ?? null;
};

const hasText = (value: string | null | undefined): boolean => {
  return (value ?? '').length > 0;
};

const fieldValue = (change: AiProposedChange, name: string): string | null => {
  const field = change.fields.find((item) => item.name === name);

  return field?.after ?? field?.before ?? null;
};

const sharedValue = (
  changes: AiProposedChange[],
  name: string
): string | null => {
  let shared: string | null = null;

  for (const change of changes) {
    const value = fieldValue(change, name);

    if (value === null) {
      return null;
    }

    if (shared !== null && shared !== value) {
      return null;
    }

    shared = value;
  }

  return shared;
};

const digestScope = (changes: AiProposedChange[]): string | null => {
  for (const name of SCOPE_FIELDS) {
    const shared = sharedValue(changes, name);

    if (shared !== null) {
      return shared;
    }
  }

  return null;
};

/** A row can only list names when every change in it actually quotes one. */
const digestNames = (changes: AiProposedChange[]): string | null => {
  const names: string[] = [];

  for (const change of changes) {
    const target = changeSummary(change).target;

    if (target === null) {
      return null;
    }

    names.push(target);
  }

  return names.join(', ');
};

const countPhrase = (
  letter: AiChangeLetter,
  count: number,
  entityType: string
): string => {
  const entities = countedEntities(entityType);

  if (letter === 'A') {
    return $localize`:Digest row covering entities a change set creates:${count}:COUNT: ${entities}:ENTITIES: created`;
  }

  if (letter === 'D') {
    return $localize`:Digest row covering entities a change set removes:${count}:COUNT: ${entities}:ENTITIES: removed`;
  }

  return $localize`:Digest row covering entities a change set updates:${count}:COUNT: ${entities}:ENTITIES: updated`;
};

const digestKey = (change: AiProposedChange): string => {
  const letter = changeLetter(change);
  const field = letter === 'M' ? (primaryField(change)?.name ?? '') : '';

  return `${letter}:${change.entityType}:${field}`;
};

const groupForDigest = (changes: AiProposedChange[]): AiDigestGroup[] => {
  const groups = new Map<string, AiDigestGroup>();

  for (const change of changes) {
    const key = digestKey(change);
    const existing = groups.get(key);

    if (existing) {
      existing.changes.push(change);

      continue;
    }

    const letter = changeLetter(change);

    groups.set(key, {
      key,
      letter,
      entityType: change.entityType,
      fieldName: letter === 'M' ? (primaryField(change)?.name ?? '') : '',
      changes: [change],
    });
  }

  return [...groups.values()];
};

const compareDigestGroups = (
  left: AiDigestGroup,
  right: AiDigestGroup
): number => {
  const byLetter = LETTER_ORDER[left.letter] - LETTER_ORDER[right.letter];

  if (byLetter !== 0) {
    return byLetter;
  }

  return right.changes.length - left.changes.length;
};

const labelOf = (lead: string, emphasis: string, trail: string): string => {
  return [lead, emphasis, trail].filter((part) => part.length > 0).join(' ');
};

/** The name is emphasised beside what happened to it, the way a many-row reads. */
const named = (
  target: string | null,
  detail: string,
  summary: string
): string => {
  if (target === null) {
    return summary;
  }

  return detail.length > 0 ? `${detail} —` : '';
};

const singleRow = (
  group: AiDigestGroup,
  change: AiProposedChange
): AiDigestRow => {
  const summary = changeSummary(change);
  const emphasis = summary.target ?? '';
  const lead = named(summary.target, summary.detail, change.summary);

  return {
    key: group.key,
    letter: group.letter,
    lead,
    emphasis,
    trail: '',
    label: change.summary,
    scope: digestScope(group.changes),
    changeIds: [change.id],
  };
};

const manyRow = (group: AiDigestGroup): AiDigestRow => {
  const count = group.changes.length;
  const entities = entityPlural(group.entityType);
  const names = group.letter === 'M' ? null : digestNames(group.changes);
  let lead = '';
  let emphasis = countPhrase(group.letter, count, group.entityType);

  if (names !== null) {
    lead =
      group.letter === 'A'
        ? $localize`:Digest row prefix before the names a change set creates:${entities}:ENTITIES: created —`
        : $localize`:Digest row prefix before the names a change set removes:${entities}:ENTITIES: removed —`;
    emphasis = names;
  } else if (group.letter === 'M' && group.fieldName.length > 0) {
    const phrase = fieldPhrase(group.fieldName);
    const covered = countedEntities(group.entityType);

    lead = $localize`:Digest row prefix before the entities a field was changed on:${phrase}:PHRASE: on`;
    emphasis = $localize`:Counts the entities one digest row covers:${count}:COUNT: ${covered}:ENTITIES:`;
  }

  return {
    key: group.key,
    letter: group.letter,
    lead,
    emphasis,
    trail: '',
    label: labelOf(lead, emphasis, ''),
    scope: digestScope(group.changes),
    changeIds: group.changes.map((change) => change.id),
  };
};

const toDigestRow = (group: AiDigestGroup): AiDigestRow => {
  const isSingle = group.changes.length === 1;

  if (isSingle) {
    return singleRow(group, group.changes[0]);
  }

  return manyRow(group);
};

const remainderRow = (groups: AiDigestGroup[]): AiDigestRow => {
  const changes = groups.flatMap((group) => group.changes);
  const count = changes.length;
  const lead = $localize`:Digest row covering the changes not itemised above:… and ${count}:COUNT: other changes`;

  return {
    key: 'other',
    letter: null,
    lead,
    emphasis: '',
    trail: '',
    label: lead,
    scope: null,
    changeIds: changes.map((change) => change.id),
  };
};

/** The label renders as three spans, so the gaps between them travel with it. */
const spaced = (row: AiDigestRow): AiDigestRow => {
  const hasLead = row.lead.length > 0 && row.emphasis.length > 0;

  return {
    ...row,
    lead: hasLead ? `${row.lead} ` : row.lead,
    trail: row.trail.length > 0 ? ` ${row.trail}` : row.trail,
  };
};

/**
 * Blocked changes are left out of the rows entirely — a checkbox that cannot
 * apply anything is worse than an honest count of what is stuck.
 */
export const digestRows = (changes: AiProposedChange[]): AiDigestRow[] => {
  const groups = groupForDigest(changes.filter(isValid));
  const ordered = [...groups].sort(compareDigestGroups);
  const fits = ordered.length <= DIGEST_ROW_LIMIT;

  if (fits) {
    return ordered.map(toDigestRow).map(spaced);
  }

  const kept = ordered.slice(0, DIGEST_ROW_LIMIT - 1);
  const rest = ordered.slice(DIGEST_ROW_LIMIT - 1);

  return [...kept.map(toDigestRow), remainderRow(rest)].map(spaced);
};

const proseLabel = (field: AiChangeField): string => {
  const label = fieldLabel(field.name);
  const hasBefore = hasText(field.before);
  const hasAfter = hasText(field.after);

  if (hasBefore && hasAfter) {
    return $localize`:Field line above a rewritten block of text:${label}:FIELD: rewritten`;
  }

  if (hasAfter) {
    return $localize`:Field line above a block of text a change adds:${label}:FIELD: added`;
  }

  return $localize`:Field line above a block of text a change clears:${label}:FIELD: cleared`;
};

/** Only the first changed line of each side; the review surface has the rest. */
const proseLines = (field: AiChangeField): AiInlineValue[] => {
  const ops = lineOps(field);
  const removed = ops.find((op) => op.kind === 'removed');
  const added = ops.find((op) => op.kind === 'added');
  const lines: AiInlineValue[] = [];

  if (removed) {
    lines.push({ mark: '−', isAdded: false, text: removed.value });
  }

  if (added) {
    lines.push({ mark: '+', isAdded: true, text: added.value });
  }

  return lines;
};

const proseField = (field: AiChangeField): AiInlineField => {
  return {
    key: field.name,
    label: proseLabel(field),
    isProse: true,
    lines: proseLines(field),
    swap: null,
    single: null,
  };
};

const scalarField = (field: AiChangeField): AiInlineField => {
  const before = field.before ?? '';
  const after = field.after ?? '';
  const isSwap = hasText(before) && hasText(after);
  const value: AiInlineValue = hasText(after)
    ? { mark: '+', isAdded: true, text: after }
    : { mark: '−', isAdded: false, text: before };

  return {
    key: field.name,
    label: fieldLabel(field.name),
    isProse: false,
    lines: [],
    swap: isSwap ? { before, after } : null,
    single: isSwap ? null : value,
  };
};

const toInlineField = (field: AiChangeField): AiInlineField => {
  const isProse = isProseField(field);

  if (isProse) {
    return proseField(field);
  }

  return scalarField(field);
};

/**
 * A creation or a deletion is about the entity itself, so it reads as one value
 * rather than as the field list behind it.
 */
const identityField = (
  change: AiProposedChange,
  letter: AiChangeLetter
): AiInlineField => {
  const summary = changeSummary(change);
  const isAdded = letter === 'A';
  const value = summary.target ?? fieldValue(change, 'name');
  const label = summary.target === null ? change.summary : summary.detail;

  return {
    key: 'identity',
    label: label.length > 0 ? label : change.summary,
    isProse: false,
    lines: [],
    swap: null,
    single: value ? { mark: isAdded ? '+' : '−', isAdded, text: value } : null,
  };
};

const inlineFields = (
  change: AiProposedChange,
  letter: AiChangeLetter
): AiInlineField[] => {
  const isIdentity = letter !== 'M';

  if (isIdentity) {
    return [identityField(change, letter)];
  }

  if (change.fields.length === 0) {
    return [identityField(change, letter)];
  }

  return change.fields.map(toInlineField);
};

export const inlineRow = (
  change: AiProposedChange,
  excludedChangeIds: Set<number>
): AiInlineRow => {
  const letter = changeLetter(change);
  const isSelectable = isValid(change);

  return {
    change,
    letter,
    isSelectable,
    isIncluded: isSelectable && !excludedChangeIds.has(change.id),
    fields: inlineFields(change, letter),
  };
};

/**
 * "TASK · NETP-511" — the entity is stated once, above its rows. An entity the
 * change set is about to create has nothing to reference yet, and its name
 * belongs on the row that creates it.
 */
export const inlineHeading = (
  label: string,
  changes: AiProposedChange[]
): string => {
  const first = changes[0];
  const quoted = first.entityId ? changeSummary(first).target : null;
  const reference = first.entitySystemId ?? quoted;

  if (!reference) {
    return label;
  }

  return `${label} · ${reference}`;
};

export interface AiAppliedRow {
  change: AiProposedChange;
  letter: AiChangeLetter;
  lead: string;
  emphasis: string;
  label: string;
  route: string[] | null;
  status: string | null;
  isFailed: boolean;
  message: string | null;
}

/** What became of one change, for the rows that report an applied set rather than propose one. */
const appliedStatus = (change: AiProposedChange): string | null => {
  if (change.undoneAt) {
    return $localize`:Marks a change that was applied and then taken back:Undone`;
  }

  if (change.applyStatus === AiChangeApplyStatus.failed) {
    return $localize`:Marks a change that could not be applied:Failed`;
  }

  if (change.applyStatus === AiChangeApplyStatus.applied) {
    return null;
  }

  return $localize`:Marks a change that was left out of an applied set:Skipped`;
};

const appliedMessage = (change: AiProposedChange): string | null => {
  const hasFailed = change.applyStatus === AiChangeApplyStatus.failed;

  if (!hasFailed) {
    return null;
  }

  return (
    change.applyError ??
    $localize`:Shown on a change that failed without saying why:This change could not be applied.`
  );
};

/**
 * An undone change points at an entity that no longer holds what the row describes, and a
 * created one may not exist at all, so only what still stands is worth linking.
 */
const appliedRoute = (
  change: AiProposedChange,
  workspace: string | null
): string[] | null => {
  if (change.undoneAt) {
    return null;
  }

  return changeRoute(change, workspace);
};

const appliedRow = (
  change: AiProposedChange,
  workspace: string | null
): AiAppliedRow => {
  const summary = changeSummary(change);
  const emphasis = summary.target ?? '';
  const lead = named(summary.target, summary.detail, change.summary);

  return {
    change,
    letter: changeLetter(change),
    lead: lead.length > 0 && emphasis.length > 0 ? `${lead} ` : lead,
    emphasis,
    label: change.summary,
    route: appliedRoute(change, workspace),
    status: appliedStatus(change),
    isFailed: change.applyStatus === AiChangeApplyStatus.failed,
    message: appliedMessage(change),
  };
};

/**
 * Every change in the set, in the order it was proposed: an applied set is a record of what
 * happened, so the ones that failed or were left out belong in it beside the ones that landed.
 */
export const appliedRows = (
  changes: readonly AiProposedChange[],
  workspace: string | null
): AiAppliedRow[] => {
  return [...changes]
    .sort((left, right) => left.sequence - right.sequence)
    .map((change) => appliedRow(change, workspace));
};
