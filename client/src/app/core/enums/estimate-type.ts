export enum EstimateType {
  storyPoints = 0,
  hours = 1,
  tShirt = 2,
}

export const estimateTypeLabels: Record<EstimateType, string> = {
  [EstimateType.storyPoints]: 'Story Points',
  [EstimateType.hours]: 'Hours',
  [EstimateType.tShirt]: 'T-Shirt',
};

export const estimateTypeUnits: Record<EstimateType, string> = {
  [EstimateType.storyPoints]: 'pts',
  [EstimateType.hours]: 'h',
  [EstimateType.tShirt]: '',
};

export const estimateTypeOptions = Object.values(EstimateType)
  .filter((v): v is EstimateType => typeof v === 'number')
  .map(value => ({ value, label: estimateTypeLabels[value] }));

export const tShirtSizes: { value: number; label: string }[] = [
  { value: 1, label: 'XS' },
  { value: 2, label: 'S' },
  { value: 3, label: 'M' },
  { value: 4, label: 'L' },
  { value: 5, label: 'XL' },
];

export function formatEstimate(type: EstimateType, value: number): string {
  if (type === EstimateType.tShirt) {
    return tShirtSizes.find(s => s.value === value)?.label ?? '?';
  }
  return `${value}${estimateTypeUnits[type]}`;
}
