import { ProjectTaskStatus } from '../enums/project-task-status';
import { AppUser } from './appuser';
import { Basemodel } from './basemodel';
import { Project } from './project';
import { Workspace } from './workspace';

export class ProjectTask extends Basemodel {

    public name: string;
    public description: string;
    public status: ProjectTaskStatus;

    public project: Project;
    public projectId: number;

    public workspace: Workspace;
    public workspaceId: number;

    public assignee: AppUser;
    public assigneeId: string;
}
