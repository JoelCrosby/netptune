import { hasPendingProviderLink } from './pending-provider-link';
import { hasPendingWorkspaceInvite } from './pending-workspace-invite';

// Linking a provider and accepting an invite both bounce through sign-in, and each has
// to be finished before the workspace list is any use.
export function routeAfterSignIn(): string[] {
  if (hasPendingProviderLink()) {
    return ['/auth/link-provider'];
  }

  if (hasPendingWorkspaceInvite()) {
    return ['/auth/accept-invite'];
  }

  return ['/workspaces'];
}
