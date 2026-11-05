const pendingWorkspaceInviteKey = 'netptune:pending-workspace-invite';

export function readPendingWorkspaceInvite(): string | null {
  return sessionStorage.getItem(pendingWorkspaceInviteKey);
}

export function writePendingWorkspaceInvite(code: string) {
  sessionStorage.setItem(pendingWorkspaceInviteKey, code);
}

export function clearPendingWorkspaceInvite() {
  sessionStorage.removeItem(pendingWorkspaceInviteKey);
}

export function hasPendingWorkspaceInvite() {
  return readPendingWorkspaceInvite() !== null;
}
