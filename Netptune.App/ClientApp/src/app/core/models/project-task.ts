import { TaskStatus } from '../enums/project-task-status';
import { AppUser } from './appuser';
import { Basemodel } from './basemodel';
import { Project } from './project';
import { Workspace } from './workspace';

export interface ProjectTask extends Basemodel {
  name: string;
  description: string;
  status: TaskStatus;

  sortOrder: number;

  project: Project;
  projectId: number;

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

  projectId: number;

  workspace: string;

  assignee?: AppUser;
  assigneeId?: string;
}
