import { AiProvider } from './ai-credential';

export interface AiModelOption {
  provider: AiProvider;
  id: string;
  label: string;
  isDefault: boolean;
}
