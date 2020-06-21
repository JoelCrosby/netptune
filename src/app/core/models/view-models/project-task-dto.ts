import { ProjectTask } from '../project-task';

export interface TaskViewModel extends ProjectTask {
  assigneeUsername: string;
  ownerUsername: string;
  assigneePictureUrl: string;
  projectName: string;
  systemId: string;
}
