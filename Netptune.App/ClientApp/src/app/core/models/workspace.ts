import { AppUser } from './appuser';
import { Project } from './project';
import { Basemodel } from './basemodel';

export interface Workspace extends Basemodel {
  name: string;
  description: string;

  users: AppUser[];
  projects: Project[];

  slug?: string;
}
