import { AssigneeViewModel } from './board-view';
import { ProjectTask } from '../project-task';

export interface TaskViewModel extends ProjectTask {
  assignees: AssigneeViewModel[];
  hasComments: boolean;
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
