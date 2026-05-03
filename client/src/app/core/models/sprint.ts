import { SprintStatus } from '../enums/sprint-status';
import { Basemodel } from './basemodel';

export interface Sprint extends Basemodel {
  name: string;
  goal?: string | null;
  status: SprintStatus;
  startDate: string;
  endDate: string;
  completedAt?: string | null;
  projectId: number;
  projectName: string;
  projectKey: string;
  workspaceId: number;
  taskCount: number;
}
