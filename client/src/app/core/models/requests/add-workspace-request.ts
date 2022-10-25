import { WorkspaceMeta } from '../workspace';

export interface AddWorkspaceRequest {
  slug: string;
  name?: string;
  description?: string;
  metaInfo: WorkspaceMeta;
}
