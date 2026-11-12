const FALLBACK_CONTROL_ID = 'form-control';

let nextControlInstance = 0;

export function nextControlInstanceId(): number {
  return nextControlInstance++;
}

export function controlIdFor(controlName: string, instanceId: number): string {
  return `${controlName || FALLBACK_CONTROL_ID}-${instanceId}`;
}

export function hintIdFor(controlId: string): string {
  return `${controlId || FALLBACK_CONTROL_ID}-hint`;
}

export function labelIdFor(controlId: string): string {
  return `${controlId || FALLBACK_CONTROL_ID}-label`;
}

export function errorIdFor(controlId: string): string {
  return `${controlId || FALLBACK_CONTROL_ID}-error`;
}

// Ids of the hint and error text describing a control, for aria-describedby.
export function describedByIds(
  controlId: string,
  hasHint: boolean,
  hasErrors: boolean
): string | null {
  const ids = [
    hasHint ? hintIdFor(controlId) : null,
    hasErrors ? errorIdFor(controlId) : null,
  ].filter((id): id is string => id !== null);

  return ids.length ? ids.join(' ') : null;
}
