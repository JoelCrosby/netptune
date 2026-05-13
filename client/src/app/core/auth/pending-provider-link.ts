export interface PendingProviderLink {
  token: string;
  provider: string;
  email: string;
}

const pendingProviderLinkKey = 'netptune:pending-provider-link';

export function readPendingProviderLink(): PendingProviderLink | null {
  const value = sessionStorage.getItem(pendingProviderLinkKey);

  if (!value) {
    return null;
  }

  try {
    return JSON.parse(value) as PendingProviderLink;
  } catch {
    clearPendingProviderLink();
    return null;
  }
}

export function writePendingProviderLink(link: PendingProviderLink) {
  sessionStorage.setItem(pendingProviderLinkKey, JSON.stringify(link));
}

export function clearPendingProviderLink() {
  sessionStorage.removeItem(pendingProviderLinkKey);
}

export function hasPendingProviderLink() {
  return readPendingProviderLink() !== null;
}
