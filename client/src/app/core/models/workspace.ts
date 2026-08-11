import { AppUser } from './appuser';
import { Project } from './project';
import { Basemodel } from './basemodel';
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
}

export interface WorkspaceMeta {
  color?: string;
  timeZone?: string;
}
