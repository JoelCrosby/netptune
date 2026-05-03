import { EstimateType } from '../enums/estimate-type';
import { TaskPriority } from '../enums/task-priority';
import { TaskStatus } from '../enums/project-task-status';
import { SprintStatus } from '../enums/sprint-status';
import { AppUser } from './appuser';
import { Basemodel } from './basemodel';
import { Project } from './project';
import { Workspace } from './workspace';

export interface ProjectTask extends Basemodel {
  name: string;
  description: string;
  status: TaskStatus;
  priority: TaskPriority | null;
  estimateType: EstimateType | null;
  estimateValue: number | null;

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
  status?: TaskStatus;

  projectId: number;
  boardGroupId?: number;

  sortOrder?: number;

  assigneeId?: string;

  priority?: TaskPriority;
  estimateType?: EstimateType;
  estimateValue?: number;
}
