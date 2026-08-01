import { AiTokenUsage } from '@core/models/ai-conversation';
import { numberFormat } from './locale';

const compact = numberFormat({
  notation: 'compact',
  maximumFractionDigits: 1,
});

export const totalTokens = (usage: AiTokenUsage | undefined): number => {
  if (!usage) {
    return 0;
  }

  return usage.inputTokens + usage.outputTokens;
};

export const formatTokens = (usage: AiTokenUsage | undefined): string => {
  return compact.format(totalTokens(usage));
};
