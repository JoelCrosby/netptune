import { ProjectTask } from './project-task';

export interface AppUser {
  id: string;

  firstname: string;
  lastname: string;
  email: string;
  userName: string;
  displayName: string;
  pictureUrl: string;
  lastLoginTime: Date;
  registrationDate: Date;
  token: string;
  tasks: ProjectTask[];
}
