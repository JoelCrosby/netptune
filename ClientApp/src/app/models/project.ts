import { Basemodel } from './basemodel';

export interface Project {

    projectId: number;
    workspaceId: number;

    name: string;
    description: string;
    projectTypeId: number;

}

export class Project extends Basemodel {

    public projectId: number;
    public workspaceId: number;

    public name: string;
    public description: string;
    public projectTypeId: number;

}
