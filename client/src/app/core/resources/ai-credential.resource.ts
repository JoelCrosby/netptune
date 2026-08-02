import { httpResource } from '@angular/common/http';
import { AiCredential, AiCredentialScope } from '../models/ai-credential';

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
