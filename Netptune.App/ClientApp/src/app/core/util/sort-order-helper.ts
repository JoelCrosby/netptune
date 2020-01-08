export function getNewSortOrder(preOrder: number, nextOrder: number) {
  if (!isNumeric(nextOrder)) {
    console.log('[REORDER] moved item to end.', { preOrder, nextOrder });
    return preOrder + 1;
  } else if (isNumeric(nextOrder) && !isNumeric(preOrder)) {
    console.log('[REORDER] moved item to start.', { preOrder, nextOrder });
    return nextOrder === 0 ? -1 : nextOrder * 0.9;
  } else {
    console.log('[REORDER] moved item to middle.', { preOrder, nextOrder });
    return (preOrder + nextOrder) / 2;
  }
}

const isNumeric = (input: any): boolean => !isNaN(input);
