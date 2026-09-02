import { EstimateType } from '@core/enums/estimate-type';
import { TaskPriority } from '@core/enums/task-priority';
import { AssigneeViewModel } from '@core/models/view-models/board-view';

export interface BulkEditTask {
  id: number;
  statusId: number;
  statusName: string;
  priority: TaskPriority | null;
  estimateType: EstimateType | null;
  estimateValue: number | null;
  dueDate?: string | null;
  projectId: number;
  sprintId?: number | null;
  sprintName?: string | null;
  tags: string[];
  assignees: AssigneeViewModel[];
}
