import { TaskStatus } from '@core/enums/project-task-status';

export interface UpdateProjectTaskRequest {
  id?: number;
  name: string;
  description: string;
  status?: TaskStatus;
  isFlagged?: boolean;
  sortOrder?: number;
  ownerId: string;
  assigneeIds?: string[];
  tags?: string[];
  projectId?: number;
}
