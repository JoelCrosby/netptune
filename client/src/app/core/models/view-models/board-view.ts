import { TaskStatus } from '@core/enums/project-task-status';
import { AppUser } from '../appuser';
import { BoardGroupType } from './board-group-view-model';
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
  type: BoardGroupType;
  sortOrder: number;
  tasks: BoardViewTask[];
}

export interface BoardViewTask {
  id: number;
  assignees: AssigneeViewModel[];
  name: string;
  systemId: string;
  tags: string[];
  isFlagged: boolean;
  sortOrder: number;
  status: TaskStatus;
  projectId: number;
  workspaceId: number;
  workspaceKey: string;
}

export interface AssigneeViewModel {
  id: string;
  displayName: string;
  pictureUrl: string;
}
