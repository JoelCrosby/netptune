import { AiChangeField, AiProposedChange } from '@core/models/ai-conversation';
import { DiffOp, lineDiff } from '@static/components/text-diff/text-diff';
import { changeKind } from './ai-assistant-change-kind';

export type { DiffMode as AiDiffMode } from '@static/components/text-diff/text-diff';

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

export const lineOps = (field: AiChangeField): DiffOp[] => {
  return lineDiff(field.before, field.after);
};
