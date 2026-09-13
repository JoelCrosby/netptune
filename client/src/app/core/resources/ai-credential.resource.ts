import { httpResource } from '@angular/common/http';
import {
  AiCredential,
  AiCredentialAvailability,
  AiCredentialScope,
} from '../models/ai-credential';

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

export const aiCredentialAvailabilityResource = () => {
  return httpResource<AiCredentialAvailability | null>(
    () => ({ url: 'api/ai/credentials/availability' }),
    { defaultValue: null }
  );
};
