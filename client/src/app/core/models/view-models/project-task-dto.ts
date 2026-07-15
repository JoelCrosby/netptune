import { AssigneeViewModel } from './board-view';
import { ProjectTask } from '../project-task';

export interface TaskViewModel extends ProjectTask {
  assignees: AssigneeViewModel[];
  hasComments: boolean;
  ownerUsername: string;
  ownerPictureUrl?: string | null;
  deletedByUsername?: string | null;
  deletedByPictureUrl?: string | null;
  projectName: string;
  systemId: string;
  workspaceKey: string;
  tags: string[];
}
