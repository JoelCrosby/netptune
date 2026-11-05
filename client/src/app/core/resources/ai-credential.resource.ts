import { httpResource } from '@angular/common/http';
import { PERMISSIONS } from '../auth/permissions';
import {
  AiCredential,
  AiCredentialAvailability,
  AiCredentialScope,
} from '../models/ai-credential';
import { permissionResource } from './permission.resource';

export const aiCredentialUrl = (scope: AiCredentialScope): string => {
  return scope === 'workspace'
    ? 'api/ai/workspace-credentials'
    : 'api/ai/credentials';
};

export const aiCredentialResource = (scope?: () => AiCredentialScope) => {
  return httpResource<AiCredential[]>(
    () => ({ url: aiCredentialUrl(scope?.() ?? 'user') }),
    { defaultValue: [] }
  );
};

export const workspaceAiCredentialResource = () => {
  return permissionResource<AiCredential[], AiCredential[]>({
    permission: PERMISSIONS.workspace.update,
    request: () => ({ url: aiCredentialUrl('workspace') }),
    defaultValue: [],
  });
};

export const aiCredentialAvailabilityResource = () => {
  return httpResource<AiCredentialAvailability | null>(
    () => ({ url: 'api/ai/credentials/availability' }),
    { defaultValue: null }
  );
};
