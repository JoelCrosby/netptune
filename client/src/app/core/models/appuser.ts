import { ProjectTask } from './project-task';
import { WorkspaceRole } from '../enums/workspace-role';

export interface AppUser {
  id: string;

  firstname: string;
  lastname: string;
  email: string;
  userName: string;
  displayName: string;
  isServiceAccount?: boolean;
  pictureUrl?: string | null;
  lastLoginTime: Date;
  registrationDate: Date;
  token: string;
  tasks: ProjectTask[];
  permissions: string[];
}

export interface WorkspaceAppUser extends AppUser {
  role: WorkspaceRole;
  isPending?: boolean;
}
