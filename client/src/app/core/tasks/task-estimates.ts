import { EstimateType } from '@core/enums/estimate-type';
import { ProjectTask } from '@core/models/project-task';

// Story points and hours are the only numeric estimate units; t-shirt sizes are categorical.
export function numericEstimateType(
  type: EstimateType | null | undefined
): EstimateType | null {
  const isNumericUnit =
    type === EstimateType.storyPoints || type === EstimateType.hours;

  return isNumericUnit ? type : null;
}

export function sumTaskEstimates(
  tasks: readonly ProjectTask[],
  type: EstimateType | null
): number {
  if (type === null) return 0;

  return tasks.reduce((total, task) => {
    const estimate = task.estimateType === type ? task.estimateValue : null;

    return total + (estimate ?? 0);
  }, 0);
}
