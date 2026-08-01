import { Workspace } from './workspace';

export interface UpdateWorkspaceResponse {
  workspace: Workspace;
  previousSlug?: string | null;
}
