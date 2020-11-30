import { ProjectTask } from '../project-task';

export interface TaskViewModel extends ProjectTask {
  assigneeUsername: string;
  assigneePictureUrl: string;
  ownerUsername: string;
  ownerPictureUrl: string;
  projectName: string;
  systemId: string;
  workspaceKey: string;
  tags: string[];
}
