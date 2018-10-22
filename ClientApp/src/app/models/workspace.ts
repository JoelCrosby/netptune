import { AppUser } from './appuser';
import { Project } from './project';
import { Basemodel } from './basemodel';

export interface Workspace {

    name: string;
    description: string;

    users: AppUser[];
    projects: Project[];
}

export class Workspace extends Basemodel {

    public id: number;

    public name: string;
    public description: string;

    public users: AppUser[];
    public projects: Project[];
}
