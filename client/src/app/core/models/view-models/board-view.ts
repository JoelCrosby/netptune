import { EstimateType } from '@core/enums/estimate-type';
import { TaskPriority } from '@core/enums/task-priority';
import { SprintStatus } from '@core/enums/sprint-status';
import { StatusCategory } from '../status';
import { AppUser } from '../appuser';
import { BoardViewModel } from './board-view-model';

export interface BoardView {
  board: BoardViewModel;
  groups: BoardViewGroup[];
  users: AppUser[];
}

export interface BoardViewGroup {
  id: number;
  name: string;
  boardId: number;
  statusId: number | null;
  sortOrder: number;
  tasks: BoardViewTask[];
}

export interface BoardViewTask {
  id: number;
  assignees: AssigneeViewModel[];
  name: string;
  systemId: string;
  tags: string[];
  priority: TaskPriority | null;
  estimateType: EstimateType | null;
  estimateValue: number | null;
  dueDate?: string | null;
  createdAt: string;
  updatedAt: string;
  sprintId?: number | null;
  sprintName?: string | null;
  sprintStatus?: SprintStatus | null;
  sortOrder: number;
  statusId: number;
  statusName: string;
  statusKey: string;
  statusColor?: string | null;
  statusCategory: StatusCategory;
  projectId: number;
  workspaceId: number;
  workspaceKey: string;
}

export interface AssigneeViewModel {
  id: string;
  displayName: string;
  pictureUrl: string;
}
