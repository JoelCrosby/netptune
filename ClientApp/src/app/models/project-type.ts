import { Basemodel } from './basemodel';

export interface ProjectType {

    id: number;
    workspaceId: number;

    name: string;
    description: string;
    typeCode: string;

}

export class ProjectType extends Basemodel {

    public id: number;
    public workspaceId: number;

    public name: string;
    public description: string;
    public typeCode: string;

}
