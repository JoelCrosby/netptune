import { httpResource } from '@angular/common/http';
import { AiCredential } from '../models/ai-credential';

export const aiCredentialResource = () => {
  return httpResource<AiCredential[]>(() => ({ url: 'api/ai/credentials' }), {
    defaultValue: [],
  });
};
