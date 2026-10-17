import { SourceControlProvider } from '@core/models/source-control';

export interface PendingSourceControlConnect {
  workspace: string;
  provider: SourceControlProvider;
}

const pendingConnectKey = 'netptune:pending-source-control-connect';

// The provider redirects back to one fixed callback URL for every workspace, so the
// workspace that started the connection has to survive the round trip here.
export function readPendingSourceControlConnect(): PendingSourceControlConnect | null {
  const value = sessionStorage.getItem(pendingConnectKey);

  if (!value) {
    return null;
  }

  try {
    return JSON.parse(value) as PendingSourceControlConnect;
  } catch {
    clearPendingSourceControlConnect();
    return null;
  }
}

export function writePendingSourceControlConnect(
  pending: PendingSourceControlConnect
) {
  sessionStorage.setItem(pendingConnectKey, JSON.stringify(pending));
}

export function clearPendingSourceControlConnect() {
  sessionStorage.removeItem(pendingConnectKey);
}
