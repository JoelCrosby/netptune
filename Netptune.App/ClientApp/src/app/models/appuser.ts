import { ProjectTask } from './project-task';

export interface AppUser {

  id: string;

  firstName: string;
  lastName: string;
  email: string;
  userName: string
  pictureUrl: string;
  lastLoginTime: Date;
  registrationDate: Date;
  tasks: ProjectTask[];
}
