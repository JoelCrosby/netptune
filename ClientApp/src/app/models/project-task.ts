import { Basemodel } from './basemodel';
import { Workspace } from './workspace';
import { Project } from './project';
import { AppUser } from './appuser';

export class ProjectTask extends Basemodel {

    public projectTaskId: number;

    public name: string;
    public description: string;
    public status: ProjectTaskStatus;

    public projectId: number;

    public project: Project;
    public workspace: Workspace;

    public assignee: AppUser;
    public assigneeId: string;
}

export enum ProjectTaskStatus {
    New,
    Complete,
    InProgress,
    OnHold,
    UnAssigned,
    AwaitingClassification,
    InActive
}
