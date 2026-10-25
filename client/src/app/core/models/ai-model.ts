import { AiProvider } from './ai-credential';
import { AiEffort } from './ai-effort';

export interface AiModelOption {
  provider: AiProvider;
  id: string;
  label: string;
  isDefault: boolean;
  isReviewDefault: boolean;
  efforts: AiEffort[];
}
