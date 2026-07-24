import { EstimateType } from '@core/enums/estimate-type';
import { TaskPriority } from '@core/enums/task-priority';

export interface UpdateProjectTaskRequest {
  id?: number;
  name: string;
  description: string;
  statusId?: number;
  sortOrder?: number;
  ownerId?: string | null;
  assigneeIds?: string[];
  tags?: string[];
  projectId?: number;
  priority?: TaskPriority | null;
  estimateType?: EstimateType | null;
  estimateValue?: number | null;
  startDate?: string | null;
  dueDate?: string | null;
}
