import { AssigneeViewModel } from './board-view';
import { ProjectTask } from '../project-task';

export interface TaskFlag {
  id: number;
  name: string;
  description: string;
  automationRuleId?: number | null;
  createdAt: string;
}

export interface TaskViewModel extends ProjectTask {
  assignees: AssigneeViewModel[];
  hasComments: boolean;
  flags: TaskFlag[];
  ownerUsername: string;
  ownerPictureUrl?: string | null;
  ownerIsServiceAccount?: boolean;
  deletedByUsername?: string | null;
  deletedByPictureUrl?: string | null;
  deletedByIsServiceAccount?: boolean;
  projectName: string;
  systemId: string;
  workspaceKey: string;
  tags: string[];
}
