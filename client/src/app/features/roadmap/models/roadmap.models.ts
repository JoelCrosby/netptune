import { SprintStatus } from '@core/enums/sprint-status';
import { TaskPriority } from '@core/enums/task-priority';
import { RelationCategory } from '@core/models/relation-type';
import { StatusCategory } from '@core/models/status';
import { AssigneeViewModel } from '@core/models/view-models/board-view';

export interface RoadmapViewModel {
  from: string;
  to: string;
  tasks: RoadmapTask[];
  relations: RoadmapRelation[];
  sprints: RoadmapSprint[];
  truncated: boolean;
}

export interface RoadmapTask {
  id: number;
  projectScopeId: number;
  systemId: string;
  name: string;
  projectId: number;
  projectName: string;
  projectKey: string;
  statusId: number;
  statusName: string;
  statusKey: string;
  statusColor?: string | null;
  statusCategory: StatusCategory;
  priority?: TaskPriority | null;
  startDate?: string | null;
  dueDate?: string | null;
  sprintId?: number | null;
  assignees: AssigneeViewModel[];
}

export interface RoadmapRelation {
  id: number;
  sourceTaskId: number;
  targetTaskId: number;
  relationTypeId: number;
  relationTypeKey: string;
  category: RelationCategory;
}

export interface RoadmapSprint {
  id: number;
  name: string;
  startDate: string;
  endDate: string;
  status: SprintStatus;
  projectId: number;
}

export interface RoadmapProjectGroup {
  id: number;
  name: string;
  tasks: RoadmapDisplayTask[];
}

export interface RoadmapDisplayTask {
  task: RoadmapTask;
  depth: number;
  parentTaskId?: number;
  hasChildren: boolean;
  blockedByCount: number;
  offscreenBlockedByCount: number;
}
