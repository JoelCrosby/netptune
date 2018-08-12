import { AppUser } from './appuser';
import { Project } from './project';

export interface Workspace {

    workspaceId: number;

    name: string;
    description: string;

    users: AppUser[];
    projects: Project[];
}
