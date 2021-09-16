import { ProjectTask } from '../project-task';

export interface TaskViewModel extends ProjectTask {
  assigneeUsername: string;
  assigneePictureUrl?: string | null;
  ownerUsername: string;
  ownerPictureUrl?: string | null;
  projectName: string;
  systemId: string;
  workspaceKey: string;
  tags: string[];
}
