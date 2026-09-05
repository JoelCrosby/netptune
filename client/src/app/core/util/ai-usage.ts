import { AiTokenUsage } from '@core/models/ai-conversation';
import { numberFormat } from './locale';

const compact = numberFormat({
  notation: 'compact',
  maximumFractionDigits: 1,
});

const exact = numberFormat({ maximumFractionDigits: 0 });

const currency = numberFormat({
  style: 'currency',
  currency: 'USD',
  maximumFractionDigits: 2,
});

export const emptyUsage: AiTokenUsage = {
  inputTokens: 0,
  outputTokens: 0,
  cacheReadTokens: 0,
  cacheCreationTokens: 0,
  cost: 0,
};

export const totalTokens = (usage: AiTokenUsage | undefined): number => {
  if (!usage) {
    return 0;
  }

  return (
    usage.inputTokens +
    usage.outputTokens +
    usage.cacheReadTokens +
    usage.cacheCreationTokens
  );
};

export const formatTokens = (usage: AiTokenUsage | undefined): string => {
  return compact.format(totalTokens(usage));
};

export const formatTokenCount = (tokens: number): string => {
  return exact.format(tokens);
};

export const formatCurrency = (value: number): string => {
  const isNegligible = value > 0 && value < 0.01;

  if (isNegligible) {
    return `<${currency.format(0.01)}`;
  }

  return currency.format(value);
};

export const formatCost = (usage: AiTokenUsage | undefined): string => {
  return formatCurrency(usage?.cost ?? 0);
};

export const sumUsage = (usages: AiTokenUsage[]): AiTokenUsage => {
  return usages.reduce((total, usage) => {
    return {
      inputTokens: total.inputTokens + usage.inputTokens,
      outputTokens: total.outputTokens + usage.outputTokens,
      cacheReadTokens: total.cacheReadTokens + usage.cacheReadTokens,
      cacheCreationTokens:
        total.cacheCreationTokens + usage.cacheCreationTokens,
      cost: total.cost + usage.cost,
    };
  }, emptyUsage);
};
