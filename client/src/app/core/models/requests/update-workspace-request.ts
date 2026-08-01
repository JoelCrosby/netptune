import { Permission } from '@core/auth/permissions';
import { WorkspaceMeta } from '../workspace';

export interface UpdateWorkspaceRequest {
  slug: string;
  newSlug?: string;
  name?: string;
  description?: string;
  metaInfo: WorkspaceMeta;
  isPublic?: boolean;
  publicPermissions?: Permission[];
}
