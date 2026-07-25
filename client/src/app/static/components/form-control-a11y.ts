const FALLBACK_CONTROL_ID = 'form-control';

export function hintIdFor(controlName: string): string {
  return `${controlName || FALLBACK_CONTROL_ID}-hint`;
}

export function labelIdFor(controlName: string): string {
  return `${controlName || FALLBACK_CONTROL_ID}-label`;
}

export function errorIdFor(controlName: string): string {
  return `${controlName || FALLBACK_CONTROL_ID}-error`;
}

/**
 * Ids of the hint and error text describing a control, for aria-describedby.
 */
export function describedByIds(
  controlName: string,
  hasHint: boolean,
  hasErrors: boolean
): string | null {
  const ids = [
    hasHint ? hintIdFor(controlName) : null,
    hasErrors ? errorIdFor(controlName) : null,
  ].filter((id): id is string => id !== null);

  return ids.length ? ids.join(' ') : null;
}
