import { EstimateType } from '../enums/estimate-type';
import { TaskPriority } from '../enums/task-priority';
import { SprintStatus } from '../enums/sprint-status';
import { StatusCategory } from './status';
import { AppUser } from './appuser';
import { Basemodel } from './basemodel';
import { Project } from './project';
import { Workspace } from './workspace';

export interface ProjectTask extends Basemodel {
  name: string;
  description: string;
  statusId: number;
  statusName: string;
  statusKey: string;
  statusColor?: string | null;
  statusCategory: StatusCategory;
  priority: TaskPriority | null;
  estimateType: EstimateType | null;
  estimateValue: number | null;
  dueDate?: string | null;

  sortOrder: number;

  project: Project;
  projectId: number;
  projectScopeId: number;

  sprintId?: number | null;
  sprintName?: string | null;
  sprintStatus?: SprintStatus | null;

  workspace: Workspace;
  workspaceId: number;

  owner: AppUser;
  ownerId: string;
}

export interface AddProjectTaskRequest {
  name: string;
  description?: string;
  statusId?: number;

  projectId: number;
  boardGroupId?: number;
  sprintId?: number | null;

  sortOrder?: number;

  assigneeId?: string;

  priority?: TaskPriority;
  estimateType?: EstimateType;
  estimateValue?: number;
  dueDate?: string | null;
}
