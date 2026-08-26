import { AppUser } from './appuser';
import { Project } from './project';
import { Basemodel } from './basemodel';
import { AssigneeViewModel } from './view-models/board-view';
import { Permission } from '../auth/permissions';

export interface Workspace extends Basemodel {
  name: string;
  description: string;
  users: AppUser[];
  projects: Project[];
  metaInfo?: WorkspaceMeta;
  slug: string;
  isPublic?: boolean;
  assistantEnabled?: boolean;
  allowAssistantDataSampling?: boolean;
  isLastVisited?: boolean;
  publicPermissions?: Permission[];
  maxUploadBytes?: number;
  // A sample of the members for an avatar stack, plus the total it is a sample
  // of. Only the workspace list endpoint populates these.
  members?: AssigneeViewModel[];
  memberCount?: number;
}

export interface WorkspaceMeta {
  color?: string;
  timeZone?: string;
  logoFileId?: string | null;
}
