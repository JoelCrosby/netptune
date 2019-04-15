import { ProjectTask } from '../project-task';

export interface ProjectTaskDto extends ProjectTask {
  id: number;
  assigneeId: string;
  name: string;
  description: string;
  status: number;
  sortOrder: number;
  projectId: number;
  workspaceId: number;
  createdAt: Date;
  updatedAt: Date;
  assigneeUsername: string;
  ownerUsername: string;
  assigneePictureUrl: string;
  projectName: string;
}
