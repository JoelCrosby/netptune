import { TaskStatus } from '../enums/project-task-status';
import { AppUser } from './appuser';
import { Basemodel } from './basemodel';
import { Project } from './project';
import { Workspace } from './workspace';

export interface ProjectTask extends Basemodel {
  name: string;
  description: string;
  status: TaskStatus;
  isFlagged: boolean;

  sortOrder: number;

  project: Project;
  projectId: number;
  projectScopeId: number;

  workspace: Workspace;
  workspaceId: number;

  assignee: AppUser;
  assigneeId: string;

  owner: AppUser;
  ownerId: string;
}

export interface AddProjectTaskRequest {
  name: string;
  description?: string;
  status?: TaskStatus;
  isFlagged?: boolean;

  projectId: number;
  boardGroupId?: number;

  sortOrder?: number;

  assigneeId?: string;
}
