import { Basemodel } from './basemodel';
import { Workspace } from './workspace';
import { Project } from './project';

export class ProjectTask extends Basemodel {

    public taskId: number;

    public name: string;
    public description: string;
    public status: ProjectTaskStatus;

    public projectId: number;

    public project: Project;
    public workspace: Workspace;
}

export enum ProjectTaskStatus {
    Complete,
    InProgress,
    OnHold,
    UnAssigned,
    AwaitingClassification
}
