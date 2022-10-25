import { WorkspaceMeta } from '../workspace';

export interface UpdateWorkspaceRequest {
  slug: string;
  name?: string;
  description?: string;
  metaInfo: WorkspaceMeta;
}
