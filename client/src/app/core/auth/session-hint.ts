const SESSION_HINT_KEY = 'Netptune-auth.session';

export function hasSessionHint(): boolean {
  try {
    return localStorage.getItem(SESSION_HINT_KEY) !== null;
  } catch {
    return true;
  }
}

export function rememberSessionHint() {
  try {
    localStorage.setItem(SESSION_HINT_KEY, 'true');
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
