import { ProjectTask } from './project-task';

export interface AppUser {
  id: string;

  firstname: string;
  lastname: string;
  email: string;
  userName: string;
  displayName: string;
  pictureUrl?: string | null;
  lastLoginTime: Date;
  registrationDate: Date;
  token: string;
  tasks: ProjectTask[];
}

export interface WorkspaceAppUser extends AppUser {
  isWorkspaceOwner: boolean;
}
