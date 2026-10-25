const countOccurrences = (text: string, passage: string): number => {
  let count = 0;
  let index = text.indexOf(passage);

  while (index >= 0) {
    count++;
    index = text.indexOf(passage, index + passage.length);
  }

  return count;
};

// Mirrors AiReviewEdits.Apply on the server, so the preview shows exactly what accepting will write.
export const applyReviewEdit = (
  currentText: string,
  find: string | null,
  replace: string
): string | null => {
  if (!find) {
    const existing = currentText.trimEnd();
    const separator = existing.length > 0 ? '\n\n' : '';

    return `${existing}${separator}${replace}`;
  }

  const matchesOnce = countOccurrences(currentText, find) === 1;

  return matchesOnce ? currentText.replace(find, () => replace) : null;
};
