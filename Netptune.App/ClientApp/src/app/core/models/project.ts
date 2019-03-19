import { Basemodel } from './basemodel';
import { Workspace } from './workspace';

export class Project extends Basemodel {

    public id: number;


    public name: string;
    public description: string;
    public repositoryUrl: string;

    public workspace: Workspace;
    public workspaceId: number;

}
