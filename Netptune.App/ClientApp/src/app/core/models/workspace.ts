import { AppUser } from './appuser';
import { Project } from './project';
import { Basemodel } from './basemodel';

export class Workspace extends Basemodel {

    public name: string;
    public description: string;

    public users: AppUser[];
    public projects: Project[];
}
