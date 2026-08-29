export enum AiEffort {
  low = 0,
  medium = 1,
  high = 2,
  xHigh = 3,
  max = 4,
}

export interface AiEffortOption {
  effort: AiEffort;
  label: string;
}
