import { ProjectTask } from './project-task';

export interface AppUser {

    firstName: string;
    lasName: string;

    pictureUrl: string;

    tasks: ProjectTask[];

}
