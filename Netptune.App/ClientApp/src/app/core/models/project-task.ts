import { ProjectTaskStatus } from '../enums/project-task-status';
import { AppUser } from './appuser';
import { Basemodel } from './basemodel';
import { Project } from './project';
import { Workspace } from './workspace';

export interface ProjectTask extends Basemodel {
  name: string;
  description: string;
  status: ProjectTaskStatus;

  project: Project;
  projectId: number;

  workspace: Workspace;
  workspaceId: number;

  assignee: AppUser;
  assigneeId: string;
}
