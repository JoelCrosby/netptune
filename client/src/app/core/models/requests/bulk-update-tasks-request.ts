import { EstimateType } from '@core/enums/estimate-type';
import { TaskPriority } from '@core/enums/task-priority';

// Each field is applied only when provided; sprint uses clearSprint to remove it.
export interface BulkUpdateTasksRequest {
  taskIds: number[];
  statusId?: number | null;
  priority?: TaskPriority | null;
  estimateType?: EstimateType | null;
  estimateValue?: number | null;
  projectId?: number | null;
  sprintId?: number | null;
  clearSprint?: boolean;
  assigneeIds?: string[] | null;
}
