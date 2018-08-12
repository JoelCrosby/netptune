import { Task } from './task';

export interface AppUser {

    firstName: string;
    lasName: string;

    pictureUrl: string;

    tasks: Task[];

}
