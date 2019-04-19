import { Basemodel } from './basemodel';
import { Workspace } from './workspace';

export interface Project extends Basemodel {
  id: number;

  name: string;
  description: string;
  repositoryUrl: string;

  workspace: Workspace;
  workspaceId: number;
}
