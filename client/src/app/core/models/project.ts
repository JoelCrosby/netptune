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

export interface AddProjectRequest {
  name: string;
  description?: string;
  repositoryUrl?: string;
  metaInfo?: ProjectMetaInfo;
  defaultStatusId?: number | null;
}

export interface ProjectMetaInfo {
  color: string;
}
