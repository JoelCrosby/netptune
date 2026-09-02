import { BulkCollectionMode } from '@core/enums/bulk-collection-mode';
import { EstimateType } from '@core/enums/estimate-type';
import { TaskPriority } from '@core/enums/task-priority';

// Each field is applied only when provided; sprint and due date use their clear flags to remove a
// value. Tags and assignees carry a mode saying whether they replace what a task holds or join it.
export interface BulkUpdateTasksRequest {
  taskIds: number[];
  statusId?: number | null;
  priority?: TaskPriority | null;
  estimateType?: EstimateType | null;
  estimateValue?: number | null;
  projectId?: number | null;
  sprintId?: number | null;
  clearSprint?: boolean;
  dueDate?: string | null;
  clearDueDate?: boolean;
  assigneeIds?: string[] | null;
  assigneeMode?: BulkCollectionMode;
  tags?: string[] | null;
  tagMode?: BulkCollectionMode;
}
