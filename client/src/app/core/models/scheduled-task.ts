import { TaskPriority } from '@core/enums/task-priority';
import { StatusCategory } from '@core/models/status';
import { AssigneeViewModel } from '@core/models/view-models/board-view';

export interface ScheduledTask {
  id: number;
  projectScopeId: number;
  systemId: string;
  name: string;
  projectId: number;
  projectName: string;
  projectKey: string;
  statusId: number;
  statusName: string;
  statusKey: string;
  statusColor?: string | null;
  statusCategory: StatusCategory;
  priority?: TaskPriority | null;
  startDate?: string | null;
  dueDate?: string | null;
  sprintId?: number | null;
  assignees: AssigneeViewModel[];
}

export interface TaskSchedule {
  startDate: string | null;
  endDate: string | null;
}

export interface ScheduledTaskChange {
  task: ScheduledTask;
  schedule: TaskSchedule;
}
