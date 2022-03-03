import { AssigneeViewModel } from './board-view';
import { ProjectTask } from '../project-task';

export interface TaskViewModel extends ProjectTask {
  assignees: AssigneeViewModel[];
  ownerUsername: string;
  ownerPictureUrl?: string | null;
  projectName: string;
  systemId: string;
  workspaceKey: string;
  tags: string[];
}
