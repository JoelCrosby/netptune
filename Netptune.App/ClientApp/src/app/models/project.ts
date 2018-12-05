import { Basemodel } from './basemodel';
import { Workspace } from './workspace';
import { ProjectType } from './project-type';

export interface Project {

    workspaceId: number;

    name: string;
    description: string;
    repositoryUrl: string;
    projectTypeId: number;

}

export class Project extends Basemodel {

    public id: number;


    public name: string;
    public description: string;
    public repositoryUrl: string;

    public ProjectType: ProjectType;
    public projectTypeId: number;

    public workspace: Workspace;
    public workspaceId: number;

}
