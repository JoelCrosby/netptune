export const getNewSortOrder = (preOrder: number, nextOrder: number) => {
  if (!isNumeric(nextOrder)) {
    // [REORDER] moved item to end.
    return preOrder + 1;
  } else if (isNumeric(nextOrder) && !isNumeric(preOrder)) {
    // [REORDER] moved item to start.
    return nextOrder === 0 ? -1 : nextOrder * 0.9;
  } else {
    // [REORDER] moved item to middle.
    return (preOrder + nextOrder) / 2;
  }
};

const isNumeric = (value: unknown): boolean => Number.isFinite(value);
