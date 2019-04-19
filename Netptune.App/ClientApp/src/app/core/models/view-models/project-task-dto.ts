import { ProjectTask } from '../project-task';

export interface ProjectTaskDto extends ProjectTask {
  assigneeUsername: string;
  ownerUsername: string;
  assigneePictureUrl: string;
  projectName: string;
}
