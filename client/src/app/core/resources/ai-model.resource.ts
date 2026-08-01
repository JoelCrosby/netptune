import { httpResource } from '@angular/common/http';
import { AiModelOption } from '../models/ai-model';

export const aiModelResource = () => {
  return httpResource<AiModelOption[]>(() => ({ url: 'api/ai/models' }), {
    defaultValue: [],
  });
};
