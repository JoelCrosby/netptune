export type DiffMode = 'split' | 'unified' | 'inline';

export type DiffOpKind = 'context' | 'added' | 'removed';

export interface DiffOp {
  kind: DiffOpKind;
  value: string;
}

export interface UnifiedLine {
  kind: DiffOpKind;
  mark: string;
  text: string;
}

export interface SplitRow {
  before: string;
  after: string;
  beforeNumber: number | null;
  afterNumber: number | null;
  beforeKind: DiffOpKind | null;
  afterKind: DiffOpKind | null;
}

export interface DiffStat {
  added: number;
  removed: number;
  label: string;
}

const EMPTY: DiffOp[] = [];

// The table is quadratic, so past this many cells the value is long enough that a word by word
// reading has stopped being useful anyway.
const TABLE_LIMIT = 1_000_000;

// Everything out, everything in: the honest answer when a diff is too big to walk.
const replaceWhole = (before: string[], after: string[]): DiffOp[] => {
  return [
    ...before.map((value): DiffOp => ({ kind: 'removed', value })),
    ...after.map((value): DiffOp => ({ kind: 'added', value })),
  ];
};

// Longest common subsequence walk, which keeps the diff stable and order preserving. A value long
// enough to blow the table out falls back to a whole replacement rather than locking the tab up.
export const diffSequence = (before: string[], after: string[]): DiffOp[] => {
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

  const ops: DiffOp[] = [];
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

export const toLines = (value: string | null | undefined): string[] => {
  const text = value ?? '';

  return text.length === 0 ? [] : text.split('\n');
};

export const toWords = (value: string | null | undefined): string[] => {
  return (value ?? '').split(/(\s+)/).filter((part) => part.length > 0);
};

// A blank line still needs a box to sit in, so it renders as a single space.
const display = (value: string): string => (value.length === 0 ? ' ' : value);

const MARKS: Record<DiffOpKind, string> = {
  added: '+',
  removed: '-',
  context: ' ',
};

export const lineDiff = (
  before: string | null | undefined,
  after: string | null | undefined
): DiffOp[] => diffSequence(toLines(before), toLines(after));

// Words are compared only inside the lines that changed, so a long page stays well under the table
// limit and unchanged lines read as context rather than as a wall of struck-through words.
export const inlineDiff = (
  before: string | null | undefined,
  after: string | null | undefined
): DiffOp[] => {
  const lines = lineDiff(before, after);
  const segments: DiffOp[] = [];
  let index = 0;

  while (index < lines.length) {
    const line = lines[index];

    if (line.kind === 'context') {
      segments.push({ kind: 'context', value: `${line.value}\n` });
      index++;

      continue;
    }

    const removed: string[] = [];
    const added: string[] = [];

    while (index < lines.length && lines[index].kind === 'removed') {
      removed.push(lines[index].value);
      index++;
    }

    while (index < lines.length && lines[index].kind === 'added') {
      added.push(lines[index].value);
      index++;
    }

    const words = diffSequence(
      toWords(removed.join('\n')),
      toWords(added.join('\n'))
    );

    segments.push(...words, { kind: 'context', value: '\n' });
  }

  return segments;
};

export const wordDiff = (
  before: string | null | undefined,
  after: string | null | undefined
): DiffOp[] => diffSequence(toWords(before), toWords(after));

export const unifiedLines = (ops: DiffOp[]): UnifiedLine[] => {
  return ops.map((op) => ({
    kind: op.kind,
    mark: MARKS[op.kind],
    text: display(op.value),
  }));
};

// Pairs each run of removals with the run of additions that replaced it, so the two columns stay
// level and a replaced line reads across rather than down.
export const splitRows = (ops: DiffOp[]): SplitRow[] => {
  const rows: SplitRow[] = [];
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

export const diffStat = (ops: DiffOp[]): DiffStat => {
  const added = ops.filter((op) => op.kind === 'added').length;
  const removed = ops.filter((op) => op.kind === 'removed').length;

  return { added, removed, label: `+${added} \u2212${removed}` };
};
