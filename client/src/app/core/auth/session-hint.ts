const SESSION_HINT_KEY = 'Netptune-auth.session';

export function hasSessionHint(): boolean {
  try {
    return localStorage.getItem(SESSION_HINT_KEY) !== null;
  } catch {
    return true;
  }
}

// The hint carries the latest token expiry so a tab can tell that another tab has
// already refreshed the shared cookies.
export function sessionHintExpiry(): string | null {
  try {
    const stored = localStorage.getItem(SESSION_HINT_KEY);
    const isDate = stored !== null && !Number.isNaN(Date.parse(stored));

    return isDate ? stored : null;
  } catch {
    return null;
  }
}

export function rememberSessionHint(expires: string) {
  try {
    localStorage.setItem(SESSION_HINT_KEY, expires);
  } catch {
    // Nothing is lost beyond the next cold start attempting a refresh.
  }
}

export function forgetSessionHint() {
  try {
    localStorage.removeItem(SESSION_HINT_KEY);
  } catch {
    // Nothing to undo — the marker simply stays as it was.
  }
}
