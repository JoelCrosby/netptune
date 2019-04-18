import { ProjectTask } from '../project-task';
import { ProjectTaskStatus } from '@app/core/enums/project-task-status';

export interface ProjectTaskDto extends ProjectTask {
  id: number;
  assigneeId: string;
  name: string;
  description: string;
  status: ProjectTaskStatus;
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
