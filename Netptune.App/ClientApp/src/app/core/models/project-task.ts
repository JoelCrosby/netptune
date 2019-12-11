import { TaskStatus } from '../enums/project-task-status';
import { AppUser } from './appuser';
import { Basemodel } from './basemodel';
import { Project } from './project';
import { Workspace } from './workspace';

export interface ProjectTask extends Basemodel {
  name: string;
  description: string;
  status: TaskStatus;

  project: Project;
  projectId: number;

  workspace: Workspace;
  workspaceId: number;

  assignee: AppUser;
  assigneeId: string;

  owner: AppUser;
  ownerId: string;
}
