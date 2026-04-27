import { EstimateType } from '@core/enums/estimate-type';
import { TaskPriority } from '@core/enums/task-priority';
import { TaskStatus } from '@core/enums/project-task-status';

export interface UpdateProjectTaskRequest {
  id?: number;
  name: string;
  description: string;
  status?: TaskStatus;
  sortOrder?: number;
  ownerId: string;
  assigneeIds?: string[];
  tags?: string[];
  projectId?: number;
  priority?: TaskPriority | null;
  estimateType?: EstimateType | null;
  estimateValue?: number | null;
}
