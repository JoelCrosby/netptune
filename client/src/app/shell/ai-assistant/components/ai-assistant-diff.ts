import { AiChangeField, AiProposedChange } from '@core/models/ai-conversation';
import { changeKind } from './ai-assistant-change-kind';

export type AiDiffMode = 'split' | 'unified' | 'inline';

export type AiDiffOpKind = 'context' | 'added' | 'removed';

export interface AiDiffOp {
  kind: AiDiffOpKind;
  value: string;
}

export interface AiUnifiedLine {
  kind: AiDiffOpKind;
  mark: string;
  text: string;
}

export interface AiSplitRow {
  before: string;
  after: string;
  beforeNumber: number | null;
  afterNumber: number | null;
  beforeKind: AiDiffOpKind | null;
  afterKind: AiDiffOpKind | null;
}

export interface AiDiffStat {
  added: number;
  removed: number;
  label: string;
}

/** Letter shown beside a change, mirroring how a source control view marks a file. */
export type AiChangeLetter = 'A' | 'M' | 'D';

/** A row is read by its letter before its text, so the colour has to travel with it. */
export const letterColour = (letter: AiChangeLetter | null): string => {
  if (letter === 'A') {
    return 'text-change-added';
  }

  if (letter === 'D') {
    return 'text-change-removed';
  }

  return 'text-change-modified';
};

export const changeLetter = (change: AiProposedChange): AiChangeLetter => {
  const kind = changeKind(change);

  if (kind === 'create') {
    return 'A';
  }

  if (kind === 'delete') {
    return 'D';
  }

  return 'M';
};

const EMPTY: AiDiffOp[] = [];

/**
 * The table is quadratic, so past this many cells the value is long enough that
 * a word by word reading has stopped being useful anyway.
 */
const TABLE_LIMIT = 1_000_000;

/** Everything out, everything in: the honest answer when a diff is too big to walk. */
const replaceWhole = (before: string[], after: string[]): AiDiffOp[] => {
  return [
    ...before.map((value): AiDiffOp => ({ kind: 'removed', value })),
    ...after.map((value): AiDiffOp => ({ kind: 'added', value })),
  ];
};

/**
 * Longest common subsequence walk, which keeps the diff stable and order
 * preserving. A value long enough to blow the table out falls back to a whole
 * replacement rather than locking the tab up.
 */
export const diffSequence = (before: string[], after: string[]): AiDiffOp[] => {
  const rows = before.length;
  const columns = after.length;

  if (rows === 0 && columns === 0) {
    return EMPTY;
  }

  if (rows * columns > TABLE_LIMIT) {
    return replaceWhole(before, after);
  }

  const table: number[][] = Array.from({ length: rows + 1 }, () => {
    return new Array<number>(columns + 1).fill(0);
  });

  for (let row = rows - 1; row >= 0; row--) {
    for (let column = columns - 1; column >= 0; column--) {
      table[row][column] =
        before[row] === after[column]
          ? table[row + 1][column + 1] + 1
          : Math.max(table[row + 1][column], table[row][column + 1]);
    }
  }

  const ops: AiDiffOp[] = [];
  let row = 0;
  let column = 0;

  while (row < rows && column < columns) {
    if (before[row] === after[column]) {
      ops.push({ kind: 'context', value: before[row] });
      row++;
      column++;

      continue;
    }

    if (table[row + 1][column] >= table[row][column + 1]) {
      ops.push({ kind: 'removed', value: before[row] });
      row++;

      continue;
    }

    ops.push({ kind: 'added', value: after[column] });
    column++;
  }

  while (row < rows) {
    ops.push({ kind: 'removed', value: before[row] });
    row++;
  }

  while (column < columns) {
    ops.push({ kind: 'added', value: after[column] });
    column++;
  }

  return ops;
};

const toLines = (value: string | null | undefined): string[] => {
  const text = value ?? '';

  return text.length === 0 ? [] : text.split('\n');
};

const toWords = (value: string | null | undefined): string[] => {
  return (value ?? '').split(/(\s+)/).filter((part) => part.length > 0);
};

/** A blank line still needs a box to sit in, so it renders as a single space. */
const display = (value: string): string => (value.length === 0 ? ' ' : value);

const MARKS: Record<AiDiffOpKind, string> = {
  added: '+',
  removed: '-',
  context: ' ',
};

export const lineOps = (field: AiChangeField): AiDiffOp[] => {
  return diffSequence(toLines(field.before), toLines(field.after));
};

export const unifiedLines = (ops: AiDiffOp[]): AiUnifiedLine[] => {
  return ops.map((op) => ({
    kind: op.kind,
    mark: MARKS[op.kind],
    text: display(op.value),
  }));
};

/**
 * Pairs each run of removals with the run of additions that replaced it, so the
 * two columns stay level and a replaced line reads across rather than down.
 */
export const splitRows = (ops: AiDiffOp[]): AiSplitRow[] => {
  const rows: AiSplitRow[] = [];
  let beforeNumber = 0;
  let afterNumber = 0;
  let index = 0;

  while (index < ops.length) {
    const op = ops[index];

    if (op.kind === 'context') {
      beforeNumber++;
      afterNumber++;

      rows.push({
        before: display(op.value),
        after: display(op.value),
        beforeNumber,
        afterNumber,
        beforeKind: 'context',
        afterKind: 'context',
      });

      index++;

      continue;
    }

    const removed: string[] = [];
    const added: string[] = [];

    while (index < ops.length && ops[index].kind === 'removed') {
      removed.push(ops[index].value);
      index++;
    }

    while (index < ops.length && ops[index].kind === 'added') {
      added.push(ops[index].value);
      index++;
    }

    const height = Math.max(removed.length, added.length);

    for (let offset = 0; offset < height; offset++) {
      const hasBefore = offset < removed.length;
      const hasAfter = offset < added.length;

      if (hasBefore) {
        beforeNumber++;
      }

      if (hasAfter) {
        afterNumber++;
      }

      rows.push({
        before: hasBefore ? display(removed[offset]) : ' ',
        after: hasAfter ? display(added[offset]) : ' ',
        beforeNumber: hasBefore ? beforeNumber : null,
        afterNumber: hasAfter ? afterNumber : null,
        beforeKind: hasBefore ? 'removed' : null,
        afterKind: hasAfter ? 'added' : null,
      });
    }
  }

  return rows;
};

export const wordSegments = (field: AiChangeField): AiDiffOp[] => {
  return diffSequence(toWords(field.before), toWords(field.after));
};

export const diffStat = (ops: AiDiffOp[]): AiDiffStat => {
  const added = ops.filter((op) => op.kind === 'added').length;
  const removed = ops.filter((op) => op.kind === 'removed').length;

  return { added, removed, label: `+${added} \u2212${removed}` };
};
