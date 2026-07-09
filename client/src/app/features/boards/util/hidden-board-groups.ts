/**
 * The `boards.hiddenGroupIds` preference is a workspace-scoped map of
 * board id -> the board-group ids hidden on that board.
 */
export type HiddenBoardGroups = Record<string, number[]>;

/** Coerce an untyped preference value into a clean board id -> group ids map. */
export function parseHiddenBoardGroups(value: unknown): HiddenBoardGroups {
  if (!value || typeof value !== 'object' || Array.isArray(value)) return {};

  const result: HiddenBoardGroups = {};

  for (const [boardId, ids] of Object.entries(value)) {
    if (!Array.isArray(ids)) continue;

    const numeric = ids.filter((id): id is number => typeof id === 'number');

    if (numeric.length) result[boardId] = numeric;
  }

  return result;
}

/** The hidden group ids for a single board. */
export function hiddenGroupIdsForBoard(
  value: unknown,
  boardId: number
): number[] {
  return parseHiddenBoardGroups(value)[boardId] ?? [];
}

/**
 * Return a new map with `boardId` set to `hiddenIds`, dropping the key when the
 * board has nothing hidden so the preference stays compact.
 */
export function withBoardHiddenGroups(
  value: unknown,
  boardId: number,
  hiddenIds: number[]
): HiddenBoardGroups {
  const next = parseHiddenBoardGroups(value);

  if (hiddenIds.length) {
    next[boardId] = hiddenIds;
  } else {
    // eslint-disable-next-line @typescript-eslint/no-dynamic-delete
    delete next[boardId];
  }

  return next;
}
